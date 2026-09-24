using FSH.Modules.MarketIntelligence.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using Modules.MarketIntelligence.Domain;
using Modules.MarketIntelligence.Services.Tsetmc;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Text.RegularExpressions;
using static FSH.Framework.BuildingBlocks.Shared.Globalization.PersianTxtNormalizer;

namespace FSH.Modules.MarketIntelligence.Services.Tsetmc;

public sealed class PriceHistoryCollectorService(
    MarketIntelligenceDbContext dbContext,
    TsetmcPriceReader priceReader,
    ILogger<PriceHistoryCollectorService> logger)
{
    public async Task<(int Inserted, int? LastDEven)> BackfillInstrumentAsync(int instrumentId, CancellationToken cancellationToken, string? insCode = null)
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
        string sourceInsCode = insCode ?? instrument.InsCode;
        IReadOnlyCollection<ClosingPriceDailyDto> history =
            await priceReader
               .GetHistoryAsync(sourceInsCode, cancellationToken)
               .ConfigureAwait(false);

        IReadOnlyCollection<ClientTypeHistoryDto> clientTypeHistory =
            await priceReader
                .GetClientTypeHistoryAsync(
                    sourceInsCode,
                    cancellationToken)
                .ConfigureAwait(false);

        IReadOnlyCollection<InstrumentShareChangeDto> shareChanges =
            await priceReader
                .GetShareChangesAsync(
                    sourceInsCode,
                    cancellationToken)
                .ConfigureAwait(false);

        int? lastDEven = history.Count == 0
                ? null
                : history.Max(x => x.DEven);

        if (lastDEven is not null &&
            logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "TSETMC history source. Symbol={Symbol}, InsCode={InsCode}, Count={Count}, LastDate={LastDate}",
                instrument.Symbol,
                sourceInsCode,
                history.Count,
                lastDEven);
        }

        Dictionary<DateOnly, ClientTypeHistoryDto> clientTypeByDate =
            clientTypeHistory
               .GroupBy(x => ToDate(x.RecDate))
               .ToDictionary(
                   g => g.Key,
                   g => g
                   .OrderByDescending(x =>
                          ToLong(x.BuyIndividualVolume) +
                          ToLong(x.BuyInstitutionalVolume) +
                          ToLong(x.SellIndividualVolume) +
                          ToLong(x.SellInstitutionalVolume))
                   .ThenByDescending(x =>
                          ToLong(x.BuyIndividualValue) +
                          ToLong(x.BuyInstitutionalValue) +
                          ToLong(x.SellIndividualValue) +
                          ToLong(x.SellInstitutionalValue))
               .First());

        HashSet<DateOnly> existingDates =
            await dbContext.DailyPrices
                .AsNoTracking()
                .Where(x =>
                    x.InstrumentId == instrument.Id)
                .Select(x => x.TradeDate)
                .ToHashSetAsync(cancellationToken)
                .ConfigureAwait(false);

        List<DailyPrice> entities = history
           .Select(x => new
             {
               Source = x,
               TradeDate = ToDate(x.DEven)
             })
           .Where(x => !existingDates.Contains(x.TradeDate))
           .Select(x =>
              {
                 clientTypeByDate.TryGetValue(
                    x.TradeDate,
                    out ClientTypeHistoryDto? clientType);

       return new DailyPrice(
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
                ToLong(x.Source.QTotCap),

                clientType is null
                    ? null
                    : ToLong(clientType.BuyIndividualVolume),

                clientType is null
                    ? null
                    : ToLong(clientType.BuyIndividualValue),

                clientType is null
                    ? null
                    : ToLong(clientType.BuyIndividualCount),

                clientType is null
                    ? null
                    : ToLong(clientType.SellIndividualVolume),

                clientType is null
                    ? null
                    : ToLong(clientType.SellIndividualValue),

                clientType is null
                    ? null
                    : ToLong(clientType.SellIndividualCount),

                clientType is null
                    ? null
                    : ToLong(clientType.BuyInstitutionalVolume),

                clientType is null
                    ? null
                    : ToLong(clientType.BuyInstitutionalValue),

                clientType is null
                    ? null
                    : ToLong(clientType.BuyInstitutionalCount),

                clientType is null
                    ? null
                    : ToLong(clientType.SellInstitutionalVolume),

                clientType is null
                    ? null
                    : ToLong(clientType.SellInstitutionalValue),

                clientType is null
                    ? null
                    : ToLong(clientType.SellInstitutionalCount));
        })
        .ToList();

        HashSet<DateOnly> existingShareChangeDates =
           await dbContext.InstrumentShareChanges
              .AsNoTracking()
              .Where(x => x.InstrumentId == instrument.Id)
              .Select(x => x.EffectiveDate)
              .ToHashSetAsync(cancellationToken)
              .ConfigureAwait(false);

        List<InstrumentShareChange> shareChangeEntities =
            shareChanges
                .Select(x => new
                {
                    Source = x,
                    EffectiveDate = ToDate(x.DEven)
                })
                .Where(x =>
                    !existingShareChangeDates.Contains(x.EffectiveDate))
                .Select(x =>
                    new InstrumentShareChange(
                        instrument.Id,
                        x.EffectiveDate,
                        ToLong(x.Source.NumberOfShareOld),
                        ToLong(x.Source.NumberOfShareNew)))
                .ToList();

        

        DateOnly observedDate = DateOnly.FromDateTime(DateTime.Today);

        

        if (entities.Count == 0 &&
            shareChangeEntities.Count == 0 )
        {
            return (0, lastDEven);
        }

        dbContext.DailyPrices.AddRange(entities);
        dbContext.InstrumentShareChanges.AddRange(shareChangeEntities);
        
        //dbContext.InstrumentShareChanges.AddRange(marketWatchItem);

        // بعد از تکمیل Valuation:
        // dbContext.InstrumentValuationHistory.AddRange(valuationEntities);

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

        return (entities.Count, lastDEven);
    }

    private async Task<IReadOnlyCollection<PriceHistoryUniverseRow>>
    GetUniverseAsync(CancellationToken cancellationToken)
    {
        List<PriceHistoryUniverseRow> universe =
            await dbContext.Database
                .SqlQueryRaw<PriceHistoryUniverseRow>(
                    """
                SELECT 
                    ci.CompanyId,
                    cm.Symbol,
                    ti.Id AS TsetmcInstrumentId
                FROM marketintelligence.CompanyIndustries ci
                INNER JOIN marketintelligence.vw_CompanyMaster cm
                    ON ci.CompanyId = cm.CompanyId
                LEFT JOIN TsetmcInstruments ti
                    ON cm.Symbol = ti.Symbol
                
                """)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        return universe;
    }
        
    public async Task<int> RunIncrementalAsync(
    CancellationToken cancellationToken,
    bool includeShareChanges = false)
    {
        MarketOverviewDto? marketOverview =
            await priceReader
                .GetMarketOverviewAsync(cancellationToken)
                .ConfigureAwait(false);

        if (marketOverview is null ||
            marketOverview.MarketActivityDEven <= 0)
        {
            logger.LogWarning(
                "TSETMC MarketOverview returned no valid market date.");

            return 0;
        }

        IReadOnlyCollection<MarketWatchItemDto> marketWatch =
            await priceReader
                .GetMarketWatchAsync(cancellationToken)
                .ConfigureAwait(false);

        DateOnly tradeDate =
            ToDate(marketOverview.MarketActivityDEven);

        Dictionary<int, DailyPrice> existingDailyPrices =
            await dbContext.DailyPrices
                .Where(x => x.TradeDate == tradeDate)
                .ToDictionaryAsync(
                    x => x.InstrumentId,
                    cancellationToken)
                .ConfigureAwait(false);

        Dictionary<int, InstrumentValuationHistory> existingValuations =
            await dbContext.InstrumentValuationHistory
                .Where(x => x.ObservedDate == tradeDate)
                .ToDictionaryAsync(
                    x => x.InstrumentId,
                    cancellationToken)
                .ConfigureAwait(false);

        IReadOnlyCollection<PriceHistoryUniverseRow> universe =
            await GetUniverseAsync(cancellationToken)
                .ConfigureAwait(false);

        int[] universeInstrumentIds =
            universe
                .Where(x => x.TsetmcInstrumentId.HasValue)
                .Select(x => x.TsetmcInstrumentId!.Value)
                .Distinct()
                .ToArray();

        List<TsetmcInstrument> universeInstruments =
            await dbContext.TsetmcInstruments
                .AsNoTracking()
                .Where(x =>
                    EF.Constant(universeInstrumentIds)
                        .Contains(x.Id) &&
                    !string.IsNullOrWhiteSpace(x.InsCode))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        Dictionary<string, int> instrumentByInsCode =
            universeInstruments
                .GroupBy(
                    x => x.InsCode,
                    StringComparer.Ordinal)
                .ToDictionary(
                    x => x.Key,
                    x => x.First().Id,
                    StringComparer.Ordinal);

        List<MarketWatchItemDto> matchedMarketWatch =
            marketWatch
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.InsCode) &&
                    instrumentByInsCode.ContainsKey(x.InsCode))
                .ToList();

        IReadOnlyCollection<ClientTypeAllItemDto> clientTypeAll =
            await priceReader
                .GetClientTypeAllAsync(cancellationToken)
                .ConfigureAwait(false);

        Dictionary<string, ClientTypeAllItemDto> clientTypeByInsCode =
            clientTypeAll
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.InsCode) &&
                    instrumentByInsCode.ContainsKey(x.InsCode))
                .GroupBy(
                    x => x.InsCode,
                    StringComparer.Ordinal)
                .ToDictionary(
                    x => x.Key,
                    x => x.First(),
                    StringComparer.Ordinal);

        int[] matchedInstrumentIds =
            matchedMarketWatch
                .Select(x => instrumentByInsCode[x.InsCode])
                .Distinct()
                .ToArray();

        Dictionary<int, DateOnly> previousTradeDates =
            await dbContext.DailyPrices
                .AsNoTracking()
                .Where(x =>
                    EF.Constant(matchedInstrumentIds)
                        .Contains(x.InstrumentId) &&
                    x.TradeDate < tradeDate)
                .GroupBy(x => x.InstrumentId)
                .Select(x => new
                {
                    InstrumentId = x.Key,
                    TradeDate = x.Max(p => p.TradeDate)
                })
                .ToDictionaryAsync(
                    x => x.InstrumentId,
                    x => x.TradeDate,
                    cancellationToken)
                .ConfigureAwait(false);

        DateOnly[] previousDates =
            previousTradeDates.Values
                .Distinct()
                .ToArray();

        List<DailyPrice> previousCandidates =
            await dbContext.DailyPrices
                .Where(x =>
                    EF.Constant(matchedInstrumentIds)
                        .Contains(x.InstrumentId) &&
                    EF.Constant(previousDates)
                        .Contains(x.TradeDate))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        Dictionary<int, DailyPrice> previousDailyPrices =
            previousCandidates
                .Where(x =>
                    previousTradeDates.TryGetValue(
                        x.InstrumentId,
                        out DateOnly previousDate) &&
                    x.TradeDate == previousDate)
                .ToDictionary(
                    x => x.InstrumentId);

        List<DailyPrice> newDailyPrices = [];
        List<InstrumentValuationHistory> newValuations = [];

        // فقط وقتی switch روشن باشد استفاده می‌شود.
        Dictionary<int, HashSet<DateOnly>>
            shareChangeDatesByInstrument = [];

        List<InstrumentShareChange> newShareChanges = [];

        if (includeShareChanges)
        {
            var existingShareChangeKeys =
                await dbContext.InstrumentShareChanges
                    .AsNoTracking()
                    .Where(x =>
                        EF.Constant(matchedInstrumentIds)
                            .Contains(x.InstrumentId))
                    .Select(x => new
                    {
                        x.InstrumentId,
                        x.EffectiveDate
                    })
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

            shareChangeDatesByInstrument =
                existingShareChangeKeys
                    .GroupBy(x => x.InstrumentId)
                    .ToDictionary(
                        g => g.Key,
                        g => g
                            .Select(x => x.EffectiveDate)
                            .ToHashSet());
        }

        foreach (MarketWatchItemDto item in matchedMarketWatch)
        {
            int instrumentId =
                instrumentByInsCode[item.InsCode];

            // -----------------------------
            // SHARE CHANGES
            // فقط وقتی switch روشن باشد
            // -----------------------------
            if (includeShareChanges)
            {
                IReadOnlyCollection<InstrumentShareChangeDto> shareChanges =
                    await priceReader
                        .GetShareChangesAsync(
                            item.InsCode,
                            cancellationToken)
                        .ConfigureAwait(false);

                if (!shareChangeDatesByInstrument.TryGetValue(
                        instrumentId,
                        out HashSet<DateOnly>? knownShareChangeDates))
                {
                    knownShareChangeDates = [];

                    shareChangeDatesByInstrument[instrumentId] =
                        knownShareChangeDates;
                }

                var groupedShareChanges =
                    shareChanges
                        .Select(x => new
                        {
                            EffectiveDate = ToDate(x.DEven),
                            OldShares = ToLong(x.NumberOfShareOld),
                            NewShares = ToLong(x.NumberOfShareNew)
                        })
                        .GroupBy(x => x.EffectiveDate);

                foreach (var group in groupedShareChanges)
                {
                    var first = group.First();

                    bool hasConflict =
                        group.Any(x =>
                            x.OldShares != first.OldShares ||
                            x.NewShares != first.NewShares);

                    if (hasConflict)
                    {
                        logger.LogWarning(
                            "TSETMC conflicting share changes skipped. InstrumentId={InstrumentId}, EffectiveDate={EffectiveDate}",
                            instrumentId,
                            first.EffectiveDate);

                        continue;
                    }

                    if (!knownShareChangeDates.Add(
                            first.EffectiveDate))
                    {
                        continue;
                    }

                    newShareChanges.Add(
                        new InstrumentShareChange(
                            instrumentId,
                            first.EffectiveDate,
                            first.OldShares,
                            first.NewShares));
                }
            }

            clientTypeByInsCode.TryGetValue(
                item.InsCode,
                out ClientTypeAllItemDto? clientType);

            if (existingDailyPrices.TryGetValue(
                    instrumentId,
                    out DailyPrice? existingDailyPrice))
            {
                existingDailyPrice.UpdateMarketSnapshot(
                    ToLong(item.PriceFirst ?? 0),
                    ToLong(item.PriceMin ?? 0),
                    ToLong(item.PriceMax ?? 0),
                    ToLong(item.PClosing ?? 0),
                    ToLong(item.PDrCotVal ?? 0),
                    ToLong(item.ZTotTran ?? 0),
                    ToLong(item.QTotTran5J ?? 0),
                    ToLong(item.QTotCap ?? 0));

                if (clientType is not null)
                {
                    existingDailyPrice.UpdateClientTypeSnapshot(
                        ToLong(clientType.BuyIndividualVolume),
                        ToLong(clientType.BuyIndividualCount),
                        ToLong(clientType.SellIndividualVolume),
                        ToLong(clientType.SellIndividualCount),
                        ToLong(clientType.BuyInstitutionalVolume),
                        ToLong(clientType.BuyInstitutionalCount),
                        ToLong(clientType.SellInstitutionalVolume),
                        ToLong(clientType.SellInstitutionalCount));
                }
            }
            else
            {
                DailyPrice newDailyPrice = new(
                    instrumentId,
                    tradeDate,
                    ToLong(item.PriceFirst ?? 0),
                    ToLong(item.PriceMin ?? 0),
                    ToLong(item.PriceMax ?? 0),
                    ToLong(item.PClosing ?? 0),
                    ToLong(item.PDrCotVal ?? 0),
                    ToLong(item.PriceYesterday ?? 0),
                    ToLong(item.ZTotTran ?? 0),
                    ToLong(item.QTotTran5J ?? 0),
                    ToLong(item.QTotCap ?? 0),

                    null, null, null,
                    null, null, null,
                    null, null, null,
                    null, null, null);

                if (clientType is not null)
                {
                    newDailyPrice.UpdateClientTypeSnapshot(
                        ToLong(clientType.BuyIndividualVolume),
                        ToLong(clientType.BuyIndividualCount),
                        ToLong(clientType.SellIndividualVolume),
                        ToLong(clientType.SellIndividualCount),
                        ToLong(clientType.BuyInstitutionalVolume),
                        ToLong(clientType.BuyInstitutionalCount),
                        ToLong(clientType.SellInstitutionalVolume),
                        ToLong(clientType.SellInstitutionalCount));
                }

                newDailyPrices.Add(newDailyPrice);
            }

            if (previousDailyPrices.TryGetValue(
                    instrumentId,
                    out DailyPrice? previousDailyPrice) &&
                (previousDailyPrice.BuyIndividualValue is null ||
                 previousDailyPrice.SellIndividualValue is null ||
                 previousDailyPrice.BuyInstitutionalValue is null ||
                 previousDailyPrice.SellInstitutionalValue is null))
            {
                IReadOnlyCollection<ClientTypeHistoryDto> history =
                    await priceReader
                        .GetClientTypeHistoryAsync(
                            item.InsCode,
                            cancellationToken)
                        .ConfigureAwait(false);

                ClientTypeHistoryDto? previousClientType =
                    history.FirstOrDefault(x =>
                        ToDate(x.RecDate) ==
                        previousDailyPrice.TradeDate);

                if (previousClientType is not null)
                {
                    previousDailyPrice.UpdateClientType(
                        ToLong(previousClientType.BuyIndividualVolume),
                        ToLong(previousClientType.BuyIndividualValue),
                        ToLong(previousClientType.BuyIndividualCount),

                        ToLong(previousClientType.SellIndividualVolume),
                        ToLong(previousClientType.SellIndividualValue),
                        ToLong(previousClientType.SellIndividualCount),

                        ToLong(previousClientType.BuyInstitutionalVolume),
                        ToLong(previousClientType.BuyInstitutionalValue),
                        ToLong(previousClientType.BuyInstitutionalCount),

                        ToLong(previousClientType.SellInstitutionalVolume),
                        ToLong(previousClientType.SellInstitutionalValue),
                        ToLong(previousClientType.SellInstitutionalCount));
                }
            }

            decimal? pe =
                ToNullableDecimal(item.PE);

            if (existingValuations.TryGetValue(
                    instrumentId,
                    out InstrumentValuationHistory? existingValuation))
            {
                existingValuation.UpdateMarketWatch(
                    item.Eps,
                    pe);
            }
            else
            {
                InstrumentValuationHistory newValuation = new(
                    instrumentId,
                    tradeDate,
                    item.Eps,
                    pe,
                    null,
                    null);

                newValuations.Add(newValuation);
            }
        }

        if (newDailyPrices.Count > 0)
        {
            dbContext.DailyPrices.AddRange(
                newDailyPrices);
        }

        if (newValuations.Count > 0)
        {
            dbContext.InstrumentValuationHistory.AddRange(
                newValuations);
        }

        if (newShareChanges.Count > 0)
        {
            dbContext.InstrumentShareChanges.AddRange(
                newShareChanges);
        }

        await dbContext
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "TSETMC incremental snapshot loaded. TradeDate={TradeDate}, MarketWatchCount={MarketWatchCount}, MatchedCount={MatchedCount}, ExistingDailyPrices={ExistingDailyPrices}, IncludeShareChanges={IncludeShareChanges}, NewShareChanges={NewShareChanges}",
                tradeDate,
                marketWatch.Count,
                matchedMarketWatch.Count,
                existingDailyPrices.Count,
                includeShareChanges,
                newShareChanges.Count);
        }

        return matchedMarketWatch.Count;
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
    public sealed class PriceHistoryUniverseRow
    {
        public int CompanyId { get; set; }

        public string Symbol { get; set; } = string.Empty;

        public int? TsetmcInstrumentId { get; set; }
    }
    private static long ToLong(decimal value) =>
        decimal.ToInt64(value);
    
    private static decimal? ToNullableDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return decimal.TryParse(
            value,
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out decimal result)
                ? result
                : null;
    }
}