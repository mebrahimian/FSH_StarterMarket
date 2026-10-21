using System.Globalization;
using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FSH.Modules.MarketIntelligence.Services.Tsetmc;

public sealed class TsetmcShareChangeCollectorService(
    MarketIntelligenceDbContext dbContext,
    TsetmcPriceReader priceReader,
    IOptions<TsetmcOptions> options,
    ILogger<TsetmcShareChangeCollectorService> logger)
{
    public Task<int> CollectAsync(
    CancellationToken cancellationToken)
    => CollectAsync(null, cancellationToken);

    public async Task<int> CollectAsync(
        int? instrumentId,
        CancellationToken cancellationToken)
    {
        string[] allowedYVals = options.Value.AllowedYVals
            .Select(x => x.ToString(CultureInfo.InvariantCulture))
            .ToArray();

        if (allowedYVals.Length == 0)
        {
            throw new InvalidOperationException(
                "Tsetmc:AllowedYVals is not configured.");
        }

        var instruments = await dbContext.TsetmcInstruments
            .AsNoTracking()
            .Where(x =>
                    allowedYVals.Contains(x.YVal) &&
                    !string.IsNullOrEmpty(x.InsCode) &&
                    (!instrumentId.HasValue || x.Id == instrumentId.Value))
            .Select(x => new { x.Id, x.Symbol, x.InsCode })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        int succeeded = 0;
        int failed = 0;
        int inserted = 0;

        foreach (var instrument in instruments)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<InstrumentShareChange> newItems = [];
            void RecordFailure(Exception exception)
            {
                failed++;

                logger.LogError(
                    exception,
                    "TSETMC share changes failed. Symbol={Symbol}, InsCode={InsCode}",
                    instrument.Symbol,
                    instrument.InsCode);
            }
            try
            {
                var source = await priceReader
                    .GetShareChangesAsync(
                        instrument.InsCode,
                        cancellationToken)
                    .ConfigureAwait(false);

                var groups = source
    .Select(x =>
    {
        if (x.NumberOfShareOld <= 0 ||
            x.NumberOfShareNew <= 0 ||
            x.NumberOfShareOld != decimal.Truncate(x.NumberOfShareOld) ||
            x.NumberOfShareNew != decimal.Truncate(x.NumberOfShareNew))
        {
            throw new InvalidOperationException(
                $"Invalid TSETMC share change. " +
                $"InsCode={instrument.InsCode}, " +
                $"DEven={x.DEven}, " +
                $"OldShares={x.NumberOfShareOld}, " +
                $"NewShares={x.NumberOfShareNew}");
        }

        return new
        {
            EffectiveDate = DateOnly.ParseExact(
                x.DEven.ToString(CultureInfo.InvariantCulture),
                "yyyyMMdd",
                CultureInfo.InvariantCulture),

            OldShares = decimal.ToInt64(x.NumberOfShareOld),

            NewShares = decimal.ToInt64(x.NumberOfShareNew)
        };
    })
    .GroupBy(x => x.EffectiveDate)
    .ToList();

                foreach (var group in groups)
                {
                    var first = group.First();

                    if (group.Any(x =>
                        x.OldShares != first.OldShares ||
                        x.NewShares != first.NewShares))
                    {
                        throw new InvalidOperationException(
                            $"Conflicting share changes. " +
                            $"InsCode={instrument.InsCode}, " +
                            $"Date={group.Key}");
                    }
                }

                HashSet<DateOnly> existingDates =
                    await dbContext.InstrumentShareChanges
                        .AsNoTracking()
                        .Where(x =>
                            x.InstrumentId == instrument.Id)
                        .Select(x => x.EffectiveDate)
                        .ToHashSetAsync(cancellationToken)
                        .ConfigureAwait(false);

                newItems = groups
                    .Where(x => !existingDates.Contains(x.Key))
                    .Select(x =>
                    {
                        var first = x.First();

                        return new InstrumentShareChange(
                            instrument.Id,
                            x.Key,
                            first.OldShares,
                            first.NewShares);
                    })
                    .ToList();

                if (newItems.Count > 0)
                {
                    dbContext.InstrumentShareChanges.AddRange(
                        newItems);

                    await dbContext
                        .SaveChangesAsync(cancellationToken)
                        .ConfigureAwait(false);
                }

                inserted += newItems.Count;
                succeeded++;
            }
            catch (OperationCanceledException)
    when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (HttpRequestException exception)
            {
                RecordFailure(exception);
            }
            catch (OperationCanceledException exception)
            {
                RecordFailure(exception);
            }
            catch (System.Text.Json.JsonException exception)
            {
                RecordFailure(exception);
            }
            catch (FormatException exception)
            {
                RecordFailure(exception);
            }
            catch (OverflowException exception)
            {
                RecordFailure(exception);
            }
            catch (InvalidOperationException exception)
            {
                RecordFailure(exception);
            }
            catch (DbUpdateException exception)
            {
                RecordFailure(exception);
            }
            finally
            {
                foreach (InstrumentShareChange item in newItems)
                {
                    dbContext.Entry(item).State =
                        EntityState.Detached;
                }
            }
            
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
            "TSETMC share changes completed. Total={Total}, Succeeded={Succeeded}, Failed={Failed}, Inserted={Inserted}",
            instruments.Count,
            succeeded,
            failed,
            inserted);
        }
        if (failed > 0)
        {
            throw new InvalidOperationException(
                $"TSETMC share changes incomplete. " +
                $"Succeeded={succeeded}, Failed={failed}, Inserted={inserted}");
        }

        return inserted;
    }
}