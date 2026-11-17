
using FSH.Modules.MarketIntelligence.Domain;

namespace FSH.Modules.MarketIntelligence.Services.Tsetmc;

public sealed record RealMoneyFlowSignalParameters(
    int LookbackDays = 20,
    decimal MinZScore = 1.5m,
    decimal MinFlowMultiple = 2m,
    decimal MinTradeValueMultiple = 1.5m);

public sealed record RealMoneyFlowSignal(
    DateOnly TradeDate,
    long NetFlow,
    decimal ZScore,
    decimal FlowMultiple,
    decimal TradeValueMultiple,
    bool IsUnusual);

public static class RealMoneyFlowSignalCalculator
{
    public static RealMoneyFlowSignal? Calculate(
        IReadOnlyCollection<DailyPrice> prices,
        RealMoneyFlowSignalParameters? parameters = null)
    {
        ArgumentNullException.ThrowIfNull(prices);

        parameters ??= new RealMoneyFlowSignalParameters();

        if (parameters.LookbackDays is < 2 or > 252)
        {
            throw new ArgumentOutOfRangeException(
                nameof(parameters),
                "LookbackDays must be between 2 and 252.");
        }

        if (parameters.MinZScore < 0 ||
            parameters.MinFlowMultiple < 0 ||
            parameters.MinTradeValueMultiple < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(parameters),
                "Signal thresholds cannot be negative.");
        }

        int requiredDays = parameters.LookbackDays + 1;

        var validPrices = prices
            .Where(x =>
                x.RealMoneyFlow.HasValue &&
                x.Value > 0)
            .OrderByDescending(x => x.TradeDate)
            .Take(requiredDays)
            .ToList();

        if (validPrices.Count < requiredDays)
            return null;

        DailyPrice current = validPrices[0];

        List<DailyPrice> history = validPrices
            .Skip(1)
            .ToList();

        decimal currentFlow =
            current.RealMoneyFlow!.Value;

        decimal averageFlow = history.Average(
            x => (decimal)x.RealMoneyFlow!.Value);

        decimal averageAbsoluteFlow = history.Average(
            x => Math.Abs(
                (decimal)x.RealMoneyFlow!.Value));

        decimal averageTradeValue = history.Average(
            x => (decimal)x.Value);

        decimal variance = history.Average(x =>
        {
            decimal difference =
                x.RealMoneyFlow!.Value - averageFlow;

            return difference * difference;
        });

        decimal standardDeviation =
            (decimal)Math.Sqrt((double)variance);

        decimal zScore = standardDeviation > 0
            ? (currentFlow - averageFlow) / standardDeviation
            : 0m;

        decimal flowMultiple = averageAbsoluteFlow > 0
            ? currentFlow / averageAbsoluteFlow
            : 0m;

        decimal tradeValueMultiple = averageTradeValue > 0
            ? current.Value / averageTradeValue
            : 0m;

        bool isUnusual =
            currentFlow > 0 &&
            zScore >= parameters.MinZScore &&
            flowMultiple >= parameters.MinFlowMultiple &&
            tradeValueMultiple >= parameters.MinTradeValueMultiple;

        return new RealMoneyFlowSignal(
            current.TradeDate,
            current.RealMoneyFlow.Value,
            Math.Round(zScore, 2),
            Math.Round(flowMultiple, 2),
            Math.Round(tradeValueMultiple, 2),
            isUnusual);
    }
}
