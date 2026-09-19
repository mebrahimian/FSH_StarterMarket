using FSH.Framework.Core.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.MarketIntelligence.Domain;

public sealed class DailyPrice :
    BaseEntity<int>,
    IGlobalEntity
{
    private DailyPrice()
    {
    }

    public DailyPrice(
        int instrumentId,
        DateOnly tradeDate,
        long firstPrice,
        long lowPrice,
        long highPrice,
        long closingPrice,
        long lastPrice,
        long yesterdayPrice,
        long tradeCount,
        long volume,
        long value)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(instrumentId);

        InstrumentId = instrumentId;
        TradeDate = tradeDate;

        FirstPrice = firstPrice;
        LowPrice = lowPrice;
        HighPrice = highPrice;
        ClosingPrice = closingPrice;
        LastPrice = lastPrice;
        YesterdayPrice = yesterdayPrice;

        TradeCount = tradeCount;
        Volume = volume;
        Value = value;
    }

    public int InstrumentId { get; private set; }

    public DateOnly TradeDate { get; private set; }

    public long FirstPrice { get; private set; }

    public long LowPrice { get; private set; }

    public long HighPrice { get; private set; }

    public long ClosingPrice { get; private set; }

    public long LastPrice { get; private set; }

    public long YesterdayPrice { get; private set; }

    public long TradeCount { get; private set; }

    public long Volume { get; private set; }

    public long Value { get; private set; }
}
