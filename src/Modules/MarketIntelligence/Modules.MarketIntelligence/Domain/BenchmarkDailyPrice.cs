using FSH.Framework.Core.Domain;

namespace Modules.MarketIntelligence.Domain;

public sealed class BenchmarkDailyPrice :
    BaseEntity<int>,
    IGlobalEntity
{
    private BenchmarkDailyPrice()
    {
    }

    public BenchmarkDailyPrice(
        int benchmarkAssetId,
        DateOnly tradeDate,
        decimal openPrice,
        decimal lowPrice,
        decimal highPrice,
        decimal closePrice)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            benchmarkAssetId);

        BenchmarkAssetId = benchmarkAssetId;
        TradeDate = tradeDate;

        OpenPrice = openPrice;
        LowPrice = lowPrice;
        HighPrice = highPrice;
        ClosePrice = closePrice;
        CreatedAt = DateTimeOffset.UtcNow;
    }
    public void Update(
    decimal lowPrice,
    decimal highPrice,
    decimal closePrice)
    {
        LowPrice = lowPrice;
        HighPrice = highPrice;
        ClosePrice = closePrice;
        LastUpdatedAt = DateTimeOffset.UtcNow;
    }
    public void UpdateFromSnapshot(
    decimal currentPrice)
    {
        if (currentPrice < LowPrice)
        {
            LowPrice = currentPrice;
        }

        if (currentPrice > HighPrice)
        {
            HighPrice = currentPrice;
        }

        ClosePrice = currentPrice;
        LastUpdatedAt = DateTimeOffset.UtcNow;
    }
    public int BenchmarkAssetId { get; private set; }

    public DateOnly TradeDate { get; private set; }

    public decimal OpenPrice { get; private set; }

    public decimal LowPrice { get; private set; }

    public decimal HighPrice { get; private set; }

    public decimal ClosePrice { get; private set; }
    public DateTimeOffset? CreatedAt { get; private set; }

    public DateTimeOffset? LastUpdatedAt { get; private set; }
}