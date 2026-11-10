using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Domain;
using FSH.Modules.MarketIntelligence.Services.Codal.Readers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modules.MarketIntelligence.Domain;
using System.Globalization;
using static FSH.Framework.BuildingBlocks.Shared.Globalization.PersianTextNormalizer;

namespace FSH.Modules.MarketIntelligence.Services.Codal.Processors;
public sealed class CapitalIncreaseRegistrationProcessor(
    CodalReportHtmlDownloader htmlDownloader,
    MarketIntelligenceDbContext dbContext,
    ILogger<CapitalIncreaseRegistrationProcessor> logger)
    : ICodalDisclosureProcessor
{
    public bool CanProcess(Disclosure disclosure)
    {
        ArgumentNullException.ThrowIfNull(disclosure);

        return !string.IsNullOrWhiteSpace(disclosure.Title) &&
               disclosure.Title.Contains(
                   "آگهی ثبت افزایش سرمایه",
                   StringComparison.Ordinal);
    }

    public async Task ProcessAsync(
        Disclosure disclosure,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(disclosure);

        if (!CanProcess(disclosure) ||
            string.IsNullOrWhiteSpace(disclosure.Url) ||
            string.IsNullOrWhiteSpace(disclosure.Symbol))
        {
            return;
        }

        // Already processed this Codal disclosure.
        bool alreadyProcessed =
            await dbContext.CapitalIncreaseEvents
                .AsNoTracking()
                .AnyAsync(
                    x => x.CodalDisclosureId == disclosure.Id,
                    cancellationToken);

        if (alreadyProcessed)
        {
            return;
        }

        string normalizedSymbol =
            NormalizeForMatch(disclosure.Symbol);

        int[] instrumentIds =
            await dbContext.TsetmcInstruments
                .AsNoTracking()
                .Where(x =>
                    x.NormalizedSymbol == normalizedSymbol ||
                    x.Symbol == disclosure.Symbol)
                .Select(x => x.Id)
                .ToArrayAsync(cancellationToken);

        if (instrumentIds.Length == 0)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(
                    "Capital registration {TracingNo}: no TSETMC instrument for {Symbol}.",
                    disclosure.TracingNo,
                    disclosure.Symbol);
            }

            return;
        }

        // Use logical capital increase events, not raw share changes.
        List<CapitalIncreaseEvent> capitalEvents =
            await dbContext.CapitalIncreaseEvents
                .Where(x => instrumentIds.Contains(x.InstrumentId))
                .ToListAsync(cancellationToken);

        if (capitalEvents.Count == 0)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(
                    "Capital registration {TracingNo}: no capital increase event for {Symbol}.",
                    disclosure.TracingNo,
                    disclosure.Symbol);
            }

            return;
        }

        string html =
            await htmlDownloader.DownloadAsync(
                disclosure.Url,
                cancellationToken);

        CapitalIncreaseRegistrationData registration =
            CapitalIncreaseRegistrationReader.Read(html);

        DateOnly assemblyDate =
            ParsePersianDate(registration.AssemblyDateRaw);

        DateOnly registrationDate =
            ParsePersianDate(registration.RegistrationDateRaw);

        // Codal capital: million IRR.
        // TSETMC shares: nominal value 1000 IRR.
        List<CapitalIncreaseEvent> matches =
            capitalEvents
                .Where(x =>
                    CapitalMatches(
                        x.OldShares,
                        registration.PreviousCapital) &&
                    CapitalMatches(
                        x.NewShares,
                        registration.NewCapital))
                .ToList();

        if (matches.Count != 1)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(
                    "Capital registration {TracingNo}: expected one capital event for {Symbol}, found {Count}.",
                    disclosure.TracingNo,
                    disclosure.Symbol,
                    matches.Count);
            }

            return;
        }

        CapitalIncreaseEvent capitalEvent = matches[0];

        if (capitalEvent.CodalDisclosureId.HasValue &&
            capitalEvent.CodalDisclosureId.Value != disclosure.Id)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(
                    "Capital event {EventId} is already linked to another disclosure.",
                    capitalEvent.Id);
            }

            return;
        }

        bool linkedElsewhere =
            await dbContext.CapitalIncreaseEvents
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.CodalDisclosureId == disclosure.Id &&
                        x.Id != capitalEvent.Id,
                    cancellationToken);

        if (linkedElsewhere)
        {
            throw new InvalidOperationException(
                $"Codal disclosure {disclosure.Id} is already linked to another capital event.");
        }

        capitalEvent.ApplyCodalRegistration(
            disclosure.Id,
            assemblyDate,
            registrationDate,
            registration.PreviousCapital,
            registration.NewCapital,
            registration.CashIncoming,
            registration.RetainedEarnings,
            registration.Reserves,
            registration.SharePremium,
            registration.RevaluationSurplus);

        await dbContext.SaveChangesAsync(cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Capital registration {TracingNo} matched capital event {EventId} for {Symbol}.",
                disclosure.TracingNo,
                capitalEvent.Id,
                disclosure.Symbol);
        }
    }


    private static bool CapitalMatches(
        long shares,
        decimal capitalMillionRials)
    {
        if (shares <= 0 || capitalMillionRials <= 0)
        {
            return false;
        }

        decimal calculatedCapital = shares / 1000m;

        // Allow rounding to whole million IRR in Codal.
        return Math.Abs(
            calculatedCapital - capitalMillionRials) <= 0.5m;
    }

    private static DateOnly ParsePersianDate(string value)
    {
        string[] parts = value.Split('/');

        if (parts.Length != 3 ||
            !int.TryParse(parts[0], out int year) ||
            !int.TryParse(parts[1], out int month) ||
            !int.TryParse(parts[2], out int day))
        {
            throw new FormatException(
                $"Invalid Persian date: {value}");
        }

        var calendar = new PersianCalendar();

        DateTime date = calendar.ToDateTime(
            year, month, day, 0, 0, 0, 0);

        return DateOnly.FromDateTime(date);
    }
}
