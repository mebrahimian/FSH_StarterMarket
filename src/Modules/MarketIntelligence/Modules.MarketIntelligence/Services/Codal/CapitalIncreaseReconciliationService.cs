
using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Domain;
using FSH.Modules.MarketIntelligence.Services.Codal.Processors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;

namespace FSH.Modules.MarketIntelligence.Services.Codal;

public sealed class CapitalIncreaseReconciliationService(
    MarketIntelligenceDbContext dbContext,
    IEnumerable<ICodalDisclosureProcessor> processors,
    ILogger<CapitalIncreaseReconciliationService> logger)
{
    private bool _codalSecurityBlocked;
    public Task ReconcileAsync(
        int instrumentId,
        int shareChangeId,
        CancellationToken cancellationToken = default)
    {
        return ReconcileInstrumentAsync(
            instrumentId,
            [shareChangeId],
            cancellationToken);
    }

    public async Task ReconcileInstrumentAsync(
        int instrumentId,
        IReadOnlyCollection<int> shareChangeIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(shareChangeIds);
        
        if (shareChangeIds.Count == 0)
        {
            return;
        }
        if (_codalSecurityBlocked)
        {
            return;
        }

        int[] targetIds = shareChangeIds
            .Distinct()
            .ToArray();

        // Read new TSETMC changes without modifying them.
        var targetChanges =
            await dbContext.InstrumentShareChanges
                .AsNoTracking()
                .Where(x =>
                    x.InstrumentId == instrumentId &&
                    targetIds.Contains(x.Id) &&
                    x.OldShares > 1 &&
                    x.NewShares > x.OldShares)
                .Select(x => new
                {
                    x.OldShares,
                    x.NewShares
                })
                .Distinct()
                .ToListAsync(cancellationToken);

        if (targetChanges.Count == 0)
        {
            return;
        }

        // Find unmatched logical capital increase events.
        var unmatchedEvents =
            await dbContext.CapitalIncreaseEvents
                .AsNoTracking()
                .Where(x =>
                    x.InstrumentId == instrumentId &&
                    x.CodalDisclosureId == null)
                .Select(x => new
                {
                    x.Id,
                    x.OldShares,
                    x.NewShares
                })
                .ToListAsync(cancellationToken);

        int[] pendingEventIds =
            unmatchedEvents
                .Where(x => targetChanges.Any(c =>
                    c.OldShares == x.OldShares &&
                    c.NewShares == x.NewShares))
                .Select(x => x.Id)
                .ToArray();

        if (pendingEventIds.Length == 0)
        {
            return;
        }

        string? symbol =
            await dbContext.TsetmcInstruments
                .AsNoTracking()
                .Where(x => x.Id == instrumentId)
                .Select(x => x.Symbol)
                .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(symbol))
        {
            return;
        }

        CapitalIncreaseRegistrationProcessor processor =
            processors
                .OfType<CapitalIncreaseRegistrationProcessor>()
                .Single();

        List<Disclosure> candidates =
            await dbContext.Disclosures
                .AsNoTracking()
                .Where(x =>
                    x.Symbol == symbol &&
                    x.Title.Contains("آگهی ثبت افزایش سرمایه") &&
                    !dbContext.CapitalIncreaseEvents.Any(
                        e => e.CodalDisclosureId == x.Id))
                .OrderByDescending(x => x.PublishDateTime)
                .ToListAsync(cancellationToken);

        foreach (Disclosure disclosure in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await processor.ProcessAsync(
                    disclosure,
                    cancellationToken);


                bool hasRemainingEvents =
                    await dbContext.CapitalIncreaseEvents
                        .AsNoTracking()
                        .AnyAsync(
                            x =>
                                pendingEventIds.Contains(x.Id) &&
                                x.CodalDisclosureId == null,
                            cancellationToken);

                if (!hasRemainingEvents)
                {
                    return;
                }

            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (HttpRequestException ex)
                when (ex.StatusCode == HttpStatusCode.TooManyRequests ||
                      (int?)ex.StatusCode == 490)
            {
                _codalSecurityBlocked = true;
                LogReconciliationFailure(ex, disclosure.TracingNo);
                return;
            }
            catch (HttpRequestException ex)
            {
                LogReconciliationFailure(ex, disclosure.TracingNo);
                return;
            }
            catch (IOException ex)
            {
                LogReconciliationFailure(ex, disclosure.TracingNo);
                return;
            }
            catch (OperationCanceledException ex)
            {
                LogReconciliationFailure(ex, disclosure.TracingNo);
                return;
            }
            catch (FormatException ex)
            {
                LogReconciliationFailure(ex, disclosure.TracingNo);
            }
            catch (InvalidOperationException ex)
            {
                LogReconciliationFailure(ex, disclosure.TracingNo);
            }
            catch (DbUpdateException ex)
            {
                LogReconciliationFailure(ex, disclosure.TracingNo);
                return;
            }
        }
    }

    private void LogReconciliationFailure(
    Exception exception,
    long tracingNo)
    {
        if (logger.IsEnabled(LogLevel.Warning))
        {
            logger.LogWarning(
                exception,
                "Capital reconciliation failed. TracingNo={TracingNo}",
                tracingNo);
        }
    }
}
