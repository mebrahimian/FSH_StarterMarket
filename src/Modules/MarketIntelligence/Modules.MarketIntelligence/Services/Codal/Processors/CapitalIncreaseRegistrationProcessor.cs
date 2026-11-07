using System.Globalization;
using static FSH.Framework.BuildingBlocks.Shared.Globalization.PersianTextNormalizer;
using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Domain;
using FSH.Modules.MarketIntelligence.Services.Codal.Readers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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

        string normalizedSymbol =
            NormalizeForMatch(disclosure.Symbol);

        // Find TSETMC instruments before requesting Codal HTML.
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
            logger.LogWarning(
                "Capital registration {TracingNo}: no TSETMC instrument for {Symbol}.",
                disclosure.TracingNo,
                disclosure.Symbol);

            return;
        }

        List<InstrumentShareChange> shareChanges =
            await dbContext.InstrumentShareChanges
                .Where(x => instrumentIds.Contains(x.InstrumentId))
                .ToListAsync(cancellationToken);

        if (shareChanges.Count == 0)
        {
            logger.LogWarning(
                "Capital registration {TracingNo}: no share change for {Symbol}.",
                disclosure.TracingNo,
                disclosure.Symbol);

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
        // TSETMC: number of shares, nominal value 1000 IRR.
        List<InstrumentShareChange> matches =
            shareChanges
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
            logger.LogWarning(
                "Capital registration {TracingNo}: expected one match for {Symbol}, found {Count}.",
                disclosure.TracingNo,
                disclosure.Symbol,
                matches.Count);

            return;
        }

        InstrumentShareChange shareChange = matches[0];

        if (shareChange.CodalDisclosureId.HasValue &&
            shareChange.CodalDisclosureId.Value != disclosure.Id)
        {
            logger.LogWarning(
                "Share change {ShareChangeId} is already linked to another disclosure.",
                shareChange.Id);

            return;
        }

        bool linkedElsewhere =
            await dbContext.InstrumentShareChanges
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.CodalDisclosureId == disclosure.Id &&
                        x.Id != shareChange.Id,
                    cancellationToken);

        if (linkedElsewhere)
        {
            throw new InvalidOperationException(
                $"Codal disclosure {disclosure.Id} is already linked to another share change.");
        }

        shareChange.ApplyCodalRegistration(
            new InstrumentShareChange.CapitalIncreaseRegistrationDetails(
                disclosure.Id,
                assemblyDate,
                registrationDate,
                registration.PreviousCapital,
                registration.NewCapital,
                registration.CashIncoming,
                registration.RetainedEarnings,
                registration.Reserves,
                registration.SharePremium,
                registration.RevaluationSurplus));

        await dbContext.SaveChangesAsync(cancellationToken);
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
            "Capital registration {TracingNo} matched share change {ShareChangeId} for {Symbol}.",
            disclosure.TracingNo,
            shareChange.Id,
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
