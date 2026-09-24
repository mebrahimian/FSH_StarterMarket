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
        long value,
        long? buyIndividualVolume = null,
        long? buyIndividualValue = null,
        long? buyIndividualCount = null,
        long? sellIndividualVolume = null,
        long? sellIndividualValue = null,
        long? sellIndividualCount = null,
        long? buyInstitutionalVolume = null,
        long? buyInstitutionalValue = null,
        long? buyInstitutionalCount = null,
        long? sellInstitutionalVolume = null,
        long? sellInstitutionalValue = null,
        long? sellInstitutionalCount = null)
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
        BuyIndividualVolume = buyIndividualVolume;
        BuyIndividualValue = buyIndividualValue;
        BuyIndividualCount = buyIndividualCount;

        SellIndividualVolume = sellIndividualVolume;
        SellIndividualValue = sellIndividualValue;
        SellIndividualCount = sellIndividualCount;

        BuyInstitutionalVolume = buyInstitutionalVolume;
        BuyInstitutionalValue = buyInstitutionalValue;
        BuyInstitutionalCount = buyInstitutionalCount;

        SellInstitutionalVolume = sellInstitutionalVolume;
        SellInstitutionalValue = sellInstitutionalValue;
        SellInstitutionalCount = sellInstitutionalCount;

        RealMoneyFlow =
            CalculateNetFlow(
                buyIndividualValue,
                sellIndividualValue);

        InstitutionalNetFlow =
            CalculateNetFlow(
                buyInstitutionalValue,
                sellInstitutionalValue);

        IndividualBuyerPower =
            CalculateIndividualBuyerPower(
                buyIndividualValue,
                buyIndividualCount,
                sellIndividualValue,
                sellIndividualCount);
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
    // Client Type - Raw
    public long? BuyIndividualVolume { get; private set; }
    public long? BuyIndividualValue { get; private set; }
    public long? BuyIndividualCount { get; private set; }

    public long? SellIndividualVolume { get; private set; }
    public long? SellIndividualValue { get; private set; }
    public long? SellIndividualCount { get; private set; }

    public long? BuyInstitutionalVolume { get; private set; }
    public long? BuyInstitutionalValue { get; private set; }
    public long? BuyInstitutionalCount { get; private set; }

    public long? SellInstitutionalVolume { get; private set; }
    public long? SellInstitutionalValue { get; private set; }
    public long? SellInstitutionalCount { get; private set; }

    // Materialized
    public long? RealMoneyFlow { get; private set; }
    public decimal? IndividualBuyerPower { get; private set; }
    public long? InstitutionalNetFlow { get; private set; }
    public void UpdateMarketSnapshot(
    long firstPrice,
    long lowPrice,
    long highPrice,
    long closingPrice,
    long lastPrice,
    long tradeCount,
    long volume,
    long value)
    {
        FirstPrice = firstPrice;
        LowPrice = lowPrice;
        HighPrice = highPrice;
        ClosingPrice = closingPrice;
        LastPrice = lastPrice;

        TradeCount = tradeCount;
        Volume = volume;
        Value = value;
    }
    public void UpdateClientType(
    long? buyIndividualVolume,
    long? buyIndividualValue,
    long? buyIndividualCount,
    long? sellIndividualVolume,
    long? sellIndividualValue,
    long? sellIndividualCount,
    long? buyInstitutionalVolume,
    long? buyInstitutionalValue,
    long? buyInstitutionalCount,
    long? sellInstitutionalVolume,
    long? sellInstitutionalValue,
    long? sellInstitutionalCount)
    {
        BuyIndividualVolume = buyIndividualVolume;
        BuyIndividualValue = buyIndividualValue;
        BuyIndividualCount = buyIndividualCount;

        SellIndividualVolume = sellIndividualVolume;
        SellIndividualValue = sellIndividualValue;
        SellIndividualCount = sellIndividualCount;

        BuyInstitutionalVolume = buyInstitutionalVolume;
        BuyInstitutionalValue = buyInstitutionalValue;
        BuyInstitutionalCount = buyInstitutionalCount;

        SellInstitutionalVolume = sellInstitutionalVolume;
        SellInstitutionalValue = sellInstitutionalValue;
        SellInstitutionalCount = sellInstitutionalCount;

        RealMoneyFlow =
            CalculateNetFlow(
                buyIndividualValue,
                sellIndividualValue);

        InstitutionalNetFlow =
            CalculateNetFlow(
                buyInstitutionalValue,
                sellInstitutionalValue);

        IndividualBuyerPower =
            CalculateIndividualBuyerPower(
                buyIndividualValue,
                buyIndividualCount,
                sellIndividualValue,
                sellIndividualCount);
    }
    public void UpdateClientTypeSnapshot(
    long? buyIndividualVolume,
    long? buyIndividualCount,
    long? sellIndividualVolume,
    long? sellIndividualCount,
    long? buyInstitutionalVolume,
    long? buyInstitutionalCount,
    long? sellInstitutionalVolume,
    long? sellInstitutionalCount)
    {
        BuyIndividualVolume = buyIndividualVolume;
        BuyIndividualCount = buyIndividualCount;

        SellIndividualVolume = sellIndividualVolume;
        SellIndividualCount = sellIndividualCount;

        BuyInstitutionalVolume = buyInstitutionalVolume;
        BuyInstitutionalCount = buyInstitutionalCount;

        SellInstitutionalVolume = sellInstitutionalVolume;
        SellInstitutionalCount = sellInstitutionalCount;

        IndividualBuyerPower =
            CalculateIndividualBuyerPower(
                BuyIndividualValue,
                BuyIndividualCount,
                SellIndividualValue,
                SellIndividualCount);
    }
    private static long? CalculateNetFlow(
    long? buyValue,
    long? sellValue)
    {
        if (!buyValue.HasValue ||
            !sellValue.HasValue)
        {
            return null;
        }

        return buyValue.Value - sellValue.Value;
    }

    private static decimal? CalculateIndividualBuyerPower(
        long? buyValue,
        long? buyCount,
        long? sellValue,
        long? sellCount)
    {
        if (!buyValue.HasValue ||
            !buyCount.HasValue ||
            !sellValue.HasValue ||
            !sellCount.HasValue ||
            buyCount.Value <= 0 ||
            sellCount.Value <= 0 ||
            sellValue.Value == 0)
        {
            return null;
        }

        decimal averageBuy =
            (decimal)buyValue.Value / buyCount.Value;

        decimal averageSell =
            (decimal)sellValue.Value / sellCount.Value;

        if (averageSell == 0)
        {
            return null;
        }

        return averageBuy / averageSell;
    }
}
