using FSH.Modules.MarketIntelligence.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modules.MarketIntelligence.Domain;
using System.Diagnostics.CodeAnalysis;

namespace FSH.Modules.MarketIntelligence.Services.Tsetmc;

[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Resolved by dependency injection.")]
internal sealed class PriceHistoryCollectorService(
    MarketIntelligenceDbContext dbContext,
    TsetmcPriceReader priceReader,
    ILogger<PriceHistoryCollectorService> logger)
{
    public async Task<int> BackfillInstrumentAsync(
        int instrumentId,
        CancellationToken cancellationToken)
    {
        TsetmcInstrument? instrument =
            await dbContext.TsetmcInstruments
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.Id == instrumentId,
                    cancellationToken)
                .ConfigureAwait(false);

        if (instrument is null)
        {
            throw new InvalidOperationException(
                $"TSETMC instrument '{instrumentId}' was not found.");
        }

        IReadOnlyCollection<ClosingPriceDailyDto> history =
            await priceReader
                .GetHistoryAsync(
                    instrument.InsCode,
                    cancellationToken)
                .ConfigureAwait(false);

        if (history.Count == 0)
        {
            return 0;
        }

        HashSet<DateOnly> existingDates =
            await dbContext.DailyPrices
                .AsNoTracking()
                .Where(x =>
                    x.InstrumentId == instrument.Id)
                .Select(x => x.TradeDate)
                .ToHashSetAsync(cancellationToken)
                .ConfigureAwait(false);

        List<DailyPrice> entities =
            history
                .Select(x => new
                {
                    Source = x,
                    TradeDate = ToDate(x.DEven)
                })
                .Where(x =>
                    !existingDates.Contains(x.TradeDate))
                .Select(x =>
                    new DailyPrice(
                        instrument.Id,
                        x.TradeDate,
                        ToLong(x.Source.PriceFirst),
                        ToLong(x.Source.PriceMin),
                        ToLong(x.Source.PriceMax),
                        ToLong(x.Source.PClosing),
                        ToLong(x.Source.PDrCotVal),
                        ToLong(x.Source.PriceYesterday),
                        ToLong(x.Source.ZTotTran),
                        ToLong(x.Source.QTotTran5J),
                        ToLong(x.Source.QTotCap)))
                .ToList();

        if (entities.Count == 0)
        {
            return 0;
        }

        dbContext.DailyPrices.AddRange(entities);

        await dbContext
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "TSETMC price history imported. InstrumentId={InstrumentId}, Symbol={Symbol}, Inserted={Inserted}",
                instrument.Id,
                instrument.Symbol,
                entities.Count);
        }

        return entities.Count;
    }

    private static DateOnly ToDate(int dEven)
    {
        int year = dEven / 10000;
        int month = (dEven / 100) % 100;
        int day = dEven % 100;

        return new DateOnly(
            year,
            month,
            day);
    }

    private static long ToLong(decimal value) =>
        decimal.ToInt64(value);
}