using FSH.Modules.MarketIntelligence.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modules.MarketIntelligence.Domain;

namespace FSH.Modules.MarketIntelligence.Services.Benchmark;

public sealed class BenchmarkPriceCollectorService(
    MarketIntelligenceDbContext dbContext,
    TgjuBenchmarkReader tgjuReader,
    TsetmcBenchmarkReader tsetmcReader,
    ILogger<BenchmarkPriceCollectorService> logger)
{
    public async Task<BenchmarkSyncResult> SyncAllAsync(CancellationToken cancellationToken)
    {
        List<BenchmarkAsset> assets =
            await dbContext.BenchmarkAssets
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    x.Source == "TGJU")
                .OrderBy(x => x.Id)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        int assetsProcessed = 0;
        int pricesInserted = 0;

        foreach (BenchmarkAsset asset in assets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            DateOnly? lastTradeDate =
                await dbContext.BenchmarkDailyPrices
                    .AsNoTracking()
                    .Where(x =>
                        x.BenchmarkAssetId == asset.Id)
                    .MaxAsync(
                        x => (DateOnly?)x.TradeDate,
                        cancellationToken)
                    .ConfigureAwait(false);

            DateOnly fromDate =
                lastTradeDate.HasValue
                    ? lastTradeDate.Value.AddDays(1)
                    : DateOnly.FromDateTime(
                        DateTime.UtcNow.AddYears(-10));

            IReadOnlyCollection<TgjuBenchmarkPriceRow> rows;

            try
            {
                rows =
                    await tgjuReader
                        .GetHistoryAsync(
                            asset.ExternalCode,
                            fromDate,
                            cancellationToken)
                        .ConfigureAwait(false);
            }
            catch (Exception ex) when (
                ex is not OperationCanceledException)
            {
                logger.LogWarning(
                    ex,
                    "TGJU benchmark sync failed. BenchmarkAssetId={BenchmarkAssetId}, Code={Code}, ExternalCode={ExternalCode}",
                    asset.Id,
                    asset.Code,
                    asset.ExternalCode);

                continue;
            }

            if (rows.Count == 0)
            {
                assetsProcessed++;
                continue;
            }

            DateOnly[] tradeDates =
                rows
                    .Select(x => x.TradeDate)
                    .Distinct()
                    .ToArray();

            HashSet<DateOnly> existingDates =
                await dbContext.BenchmarkDailyPrices
                    .AsNoTracking()
                    .Where(x =>
                        x.BenchmarkAssetId == asset.Id &&
                        EF.Constant(tradeDates)
                            .Contains(x.TradeDate))
                    .Select(x => x.TradeDate)
                    .ToHashSetAsync(cancellationToken)
                    .ConfigureAwait(false);

            List<BenchmarkDailyPrice> newPrices =
                rows
                    .Where(x =>
                        !existingDates.Contains(x.TradeDate))
                    .GroupBy(x => x.TradeDate)
                    .Select(x => x.First())
                    .Select(x =>
                        new BenchmarkDailyPrice(
                            asset.Id,
                            x.TradeDate,
                            x.OpenPrice,
                            x.LowPrice,
                            x.HighPrice,
                            x.ClosePrice))
                    .ToList();

            if (newPrices.Count > 0)
            {
                dbContext.BenchmarkDailyPrices.AddRange(
                    newPrices);

                await dbContext
                    .SaveChangesAsync(cancellationToken)
                    .ConfigureAwait(false);

                pricesInserted += newPrices.Count;
            }
            assetsProcessed++;
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                "TGJU benchmark synced. Code={Code}, Rows={Rows}, Inserted={Inserted}, FromDate={FromDate}",
                asset.Code,
                rows.Count,
                newPrices.Count,
                fromDate);
            }
        }

        return new BenchmarkSyncResult(
            assets.Count,
            assetsProcessed,
            pricesInserted);
    }
    public async Task<BenchmarkIncrementalSyncResult> SyncTgjuIncrementalAsync(CancellationToken cancellationToken)
    {
        List<BenchmarkAsset> assets =
            await dbContext.BenchmarkAssets
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    x.Source == "TGJU")
                .OrderBy(x => x.Id)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        int assetsProcessed = 0;
        int inserted = 0;
        int updated = 0;

        foreach (BenchmarkAsset asset in assets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            bool isTether = string.Equals(asset.ExternalCode, "crypto-tether",StringComparison.OrdinalIgnoreCase);
            
            TgjuBenchmarkPriceRow? row;
            try
            {                
                row = await tgjuReader
                        .GetCurrentAsync(
                            asset.ExternalCode,
                            cancellationToken)
                        .ConfigureAwait(false);
            }
            catch (Exception ex) when (
                ex is not OperationCanceledException)
            {
                logger.LogWarning(
                    ex,
                    "TGJU incremental benchmark sync failed. BenchmarkAssetId={BenchmarkAssetId}, Code={Code}",
                    asset.Id,
                    asset.Code);

                continue;
            }

            if (row is null)
            {
                continue;
            }

            BenchmarkDailyPrice? existing =
                await dbContext.BenchmarkDailyPrices
                    .SingleOrDefaultAsync(
                        x =>
                            x.BenchmarkAssetId == asset.Id &&
                            x.TradeDate == row.TradeDate,
                        cancellationToken)
                    .ConfigureAwait(false);

            if (existing is null)
            {
                decimal currentPrice = row.ClosePrice;

                dbContext.BenchmarkDailyPrices.Add(
                    new BenchmarkDailyPrice(
                        asset.Id,
                        row.TradeDate,
                        currentPrice,
                        currentPrice,
                        currentPrice,
                        currentPrice));

                inserted++;
            }
            else
            {
                if (!isTether)
                {
                    existing.Update(
                            row.LowPrice,
                            row.HighPrice,
                            row.ClosePrice);
                }
                else
                {
                    existing.UpdateFromSnapshot(row.ClosePrice);
                }
                

                updated++;
            }

            await dbContext
                .SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);

            assetsProcessed++;
        }

        return new BenchmarkIncrementalSyncResult(
            assets.Count,
            assetsProcessed,
            inserted,
            updated);
    }
    public async Task<BenchmarkSyncResult> BackfillTgjuHistoryAsync(DateOnly fromDate, CancellationToken cancellationToken)
    {
        List<BenchmarkAsset> assets =
            await dbContext.BenchmarkAssets
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    x.Source == "TGJU" &&
                    x.Code != "USDT_IRR")
                .OrderBy(x => x.Id)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        int assetsProcessed = 0;
        int pricesInserted = 0;

        foreach (BenchmarkAsset asset in assets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyCollection<TgjuBenchmarkPriceRow> rows;

            try
            {
                rows =
                    await tgjuReader
                        .GetHistoryAsync(
                            asset.ExternalCode,
                            fromDate,
                            cancellationToken)
                        .ConfigureAwait(false);
            }
            catch (Exception ex) when (
                ex is not OperationCanceledException)
            {
                logger.LogWarning(
                    ex,
                    "TGJU benchmark history failed. BenchmarkAssetId={BenchmarkAssetId}, Code={Code}",
                    asset.Id,
                    asset.Code);

                continue;
            }

            if (rows.Count == 0)
            {
                assetsProcessed++;
                continue;
            }

            DateOnly[] tradeDates =
                rows
                    .Select(x => x.TradeDate)
                    .Distinct()
                    .ToArray();

            DateOnly minTradeDate = tradeDates.Min();
            DateOnly maxTradeDate = tradeDates.Max();

            HashSet<DateOnly> existingDates =
                await dbContext.BenchmarkDailyPrices
                    .AsNoTracking()
                    .Where(x =>
                        x.BenchmarkAssetId == asset.Id &&
                        x.TradeDate >= minTradeDate &&
                        x.TradeDate <= maxTradeDate)
                    .Select(x => x.TradeDate)
                    .ToHashSetAsync(cancellationToken)
                    .ConfigureAwait(false);

            List<BenchmarkDailyPrice> newPrices =
                rows
                    .Where(x =>
                        !existingDates.Contains(x.TradeDate))
                    .GroupBy(x => x.TradeDate)
                    .Select(x => x.First())
                    .Select(x =>
                        new BenchmarkDailyPrice(
                            asset.Id,
                            x.TradeDate,
                            x.OpenPrice,
                            x.LowPrice,
                            x.HighPrice,
                            x.ClosePrice))
                    .ToList();

            if (newPrices.Count > 0)
            {
                dbContext.BenchmarkDailyPrices.AddRange(
                    newPrices);

                await dbContext
                    .SaveChangesAsync(cancellationToken)
                    .ConfigureAwait(false);

                pricesInserted += newPrices.Count;
            }

            assetsProcessed++;
        }

        return new BenchmarkSyncResult(
            assets.Count,
            assetsProcessed,
            pricesInserted);
    }
    public async Task<BenchmarkSyncResult> BackfillTsetmcHistoryAsync(CancellationToken cancellationToken)
    {
        List<BenchmarkAsset> assets =
            await dbContext.BenchmarkAssets
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    x.Source == "TSETMC")
                .OrderBy(x => x.Id)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        int assetsProcessed = 0;
        int pricesInserted = 0;

        foreach (BenchmarkAsset asset in assets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyCollection<TsetmcBenchmarkPriceRow> rows;

            try
            {
                rows =
                    await tsetmcReader
                        .GetHistoryAsync(
                            asset.ExternalCode,
                            cancellationToken)
                        .ConfigureAwait(false);
            }
            catch (Exception ex) when (
                ex is not OperationCanceledException)
            {
                logger.LogWarning(
                    ex,
                    "TSETMC benchmark history failed. BenchmarkAssetId={BenchmarkAssetId}, Code={Code}",
                    asset.Id,
                    asset.Code);

                continue;
            }

            if (rows.Count == 0)
            {
                assetsProcessed++;
                continue;
            }

            DateOnly minTradeDate =
                rows.Min(x => x.TradeDate);

            DateOnly maxTradeDate =
                rows.Max(x => x.TradeDate);

            HashSet<DateOnly> existingDates =
                await dbContext.BenchmarkDailyPrices
                    .AsNoTracking()
                    .Where(x =>
                        x.BenchmarkAssetId == asset.Id &&
                        x.TradeDate >= minTradeDate &&
                        x.TradeDate <= maxTradeDate)
                    .Select(x => x.TradeDate)
                    .ToHashSetAsync(cancellationToken)
                    .ConfigureAwait(false);

            List<BenchmarkDailyPrice> newPrices =
                rows
                    .Where(x =>
                        !existingDates.Contains(x.TradeDate))
                    .GroupBy(x => x.TradeDate)
                    .Select(x => x.First())
                    .Select(x =>
                        new BenchmarkDailyPrice(
                            asset.Id,
                            x.TradeDate,
                            x.ClosePrice,
                            x.ClosePrice,
                            x.ClosePrice,
                            x.ClosePrice))
                    .ToList();

            const int batchSize = 100;

            foreach (BenchmarkDailyPrice[] batch in
                newPrices.Chunk(batchSize))
            {
                dbContext.BenchmarkDailyPrices.AddRange(batch);

                await dbContext
                    .SaveChangesAsync(cancellationToken)
                    .ConfigureAwait(false);

                dbContext.ChangeTracker.Clear();

                pricesInserted += batch.Length;
            }

            assetsProcessed++;
        }

        return new BenchmarkSyncResult(
            assets.Count,
            assetsProcessed,
            pricesInserted);
    }
    public async Task<BenchmarkSyncResult> SyncTsetmcIncrementalAsync(CancellationToken cancellationToken)
    {
        List<BenchmarkAsset> assets =
            await dbContext.BenchmarkAssets
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    x.Source == "TSETMC")
                .OrderBy(x => x.Id)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        IReadOnlyCollection<TsetmcBenchmarkCurrentRow> currentRows =
            await tsetmcReader
                .GetCurrentAsync(cancellationToken)
                .ConfigureAwait(false);

        int assetsProcessed = 0;
        int affectedRows = 0;
        int pricesInserted = 0;

        foreach (BenchmarkAsset asset in assets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            TsetmcBenchmarkCurrentRow? current =
                currentRows.FirstOrDefault(
                    x => x.InsCode == asset.ExternalCode);

            if (current is null)
            {
                continue;
            }

            DateOnly? tradeDate =
               await dbContext.DailyPrices
                 .AsNoTracking()
                 .Where(x => x.Volume > 0)
                 .OrderByDescending(x => x.TradeDate)
                 .Select(x => (DateOnly?)x.TradeDate)
                 .FirstOrDefaultAsync(cancellationToken)
                 .ConfigureAwait(false);

            if (tradeDate is null)
            {
                return new BenchmarkSyncResult(
                    assets.Count,
                    0,
                    0);
            }           

            BenchmarkDailyPrice? existing =
                await dbContext.BenchmarkDailyPrices
                    .SingleOrDefaultAsync(
                        x =>
                            x.BenchmarkAssetId == asset.Id &&
                            x.TradeDate == tradeDate.Value,
                        cancellationToken)
                    .ConfigureAwait(false);

            if (existing is null)
            {
                BenchmarkDailyPrice price = new(
                    asset.Id,
                    tradeDate.Value,
                    current.CurrentValue,
                    current.LowValue,
                    current.HighValue,
                    current.CurrentValue);

                dbContext.BenchmarkDailyPrices.Add(price);

                pricesInserted++;
            }
            else
            {
                existing.Update(current.LowValue, current.HighValue, current.CurrentValue);
            }

            assetsProcessed++;
            affectedRows++;
        }

        await dbContext
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return new BenchmarkSyncResult(
            assets.Count,
            assetsProcessed,
            affectedRows);
    }

}


public sealed record BenchmarkSyncResult(
    int AssetsFound,
    int AssetsProcessed,
    int PricesInserted);
public sealed record BenchmarkIncrementalSyncResult(
    int AssetsFound,
    int AssetsProcessed,
    int Inserted,
    int Updated);