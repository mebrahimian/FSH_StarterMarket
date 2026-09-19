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
    }

    public int BenchmarkAssetId { get; private set; }

    public DateOnly TradeDate { get; private set; }

    public decimal OpenPrice { get; private set; }

    public decimal LowPrice { get; private set; }

    public decimal HighPrice { get; private set; }

    public decimal ClosePrice { get; private set; }
}