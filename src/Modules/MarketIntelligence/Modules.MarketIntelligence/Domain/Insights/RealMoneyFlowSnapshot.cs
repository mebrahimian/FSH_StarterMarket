
using FSH.Framework.Core.Domain;

namespace FSH.Modules.MarketIntelligence.Domain.Insights;

/// <summary>
/// One mutable monthly snapshot per company and Persian calendar month.
/// </summary>
public sealed class RealMoneyFlowSnapshot : BaseEntity<Guid>, IGlobalEntity
{
    private RealMoneyFlowSnapshot() { }

    public RealMoneyFlowSnapshot(
        int companyId,
        string symbol,
        string periodEndDate,
        RealMoneyFlowSnapshotMetrics metrics)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(companyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(periodEndDate);

        CompanyId = companyId;
        Symbol = symbol;
        PeriodEndDate = periodEndDate;

        Apply(metrics);
    }

    public int CompanyId { get; private set; }
    public string Symbol { get; private set; } = string.Empty;
    public string PeriodEndDate { get; private set; } = string.Empty;

    // Current month
    public decimal? CurrentMonthInflow { get; private set; }
    public decimal? CurrentMonthOutflow { get; private set; }
    public decimal? CurrentMonthNetFlow { get; private set; }

    // Same month of previous year
    public decimal? PreviousYearCurrentMonthInflow { get; private set; }
    public decimal? PreviousYearCurrentMonthOutflow { get; private set; }
    public decimal? PreviousYearCurrentMonthNetFlow { get; private set; }

    // Rolling three months
    public decimal? RollingThreeMonthInflow { get; private set; }
    public decimal? RollingThreeMonthOutflow { get; private set; }
    public decimal? RollingThreeMonthNetFlow { get; private set; }

    public decimal? PreviousYearRollingThreeMonthInflow { get; private set; }
    public decimal? PreviousYearRollingThreeMonthOutflow { get; private set; }
    public decimal? PreviousYearRollingThreeMonthNetFlow { get; private set; }

    // Rolling six months
    public decimal? RollingSixMonthInflow { get; private set; }
    public decimal? RollingSixMonthOutflow { get; private set; }
    public decimal? RollingSixMonthNetFlow { get; private set; }

    public decimal? PreviousYearRollingSixMonthInflow { get; private set; }
    public decimal? PreviousYearRollingSixMonthOutflow { get; private set; }
    public decimal? PreviousYearRollingSixMonthNetFlow { get; private set; }

    // Persian calendar YTD
    public decimal? YearToDateInflow { get; private set; }
    public decimal? YearToDateOutflow { get; private set; }
    public decimal? YearToDateNetFlow { get; private set; }

    public decimal? PreviousYearToDateInflow { get; private set; }
    public decimal? PreviousYearToDateOutflow { get; private set; }
    public decimal? PreviousYearToDateNetFlow { get; private set; }

    // Year-over-year comparisons
    public decimal? MonthlyInflowYoYGrowthPercent { get; private set; }
    public decimal? MonthlyOutflowYoYGrowthPercent { get; private set; }
    public decimal? MonthlyNetFlowYoYChange { get; private set; }

    public decimal? ThreeMonthInflowYoYGrowthPercent { get; private set; }
    public decimal? ThreeMonthOutflowYoYGrowthPercent { get; private set; }
    public decimal? ThreeMonthNetFlowYoYChange { get; private set; }

    public decimal? SixMonthInflowYoYGrowthPercent { get; private set; }
    public decimal? SixMonthOutflowYoYGrowthPercent { get; private set; }
    public decimal? SixMonthNetFlowYoYChange { get; private set; }

    public decimal? YearToDateInflowYoYGrowthPercent { get; private set; }
    public decimal? YearToDateOutflowYoYGrowthPercent { get; private set; }
    public decimal? YearToDateNetFlowYoYChange { get; private set; }

    // Historical 3M - excludes current month
    public decimal? ThreeMonthInflowAverage { get; private set; }
    public decimal? ThreeMonthInflowHigh { get; private set; }
    public string? ThreeMonthInflowHighPeriod { get; private set; }
    public decimal? ThreeMonthInflowLow { get; private set; }
    public string? ThreeMonthInflowLowPeriod { get; private set; }

    public decimal? ThreeMonthOutflowAverage { get; private set; }
    public decimal? ThreeMonthOutflowHigh { get; private set; }
    public string? ThreeMonthOutflowHighPeriod { get; private set; }
    public decimal? ThreeMonthOutflowLow { get; private set; }
    public string? ThreeMonthOutflowLowPeriod { get; private set; }

    public decimal? ThreeMonthNetFlowAverage { get; private set; }
    public decimal? ThreeMonthNetFlowHigh { get; private set; }
    public string? ThreeMonthNetFlowHighPeriod { get; private set; }
    public decimal? ThreeMonthNetFlowLow { get; private set; }
    public string? ThreeMonthNetFlowLowPeriod { get; private set; }

    public decimal? ThreeMonthCurrentInflowVsAveragePercent { get; private set; }
    public decimal? ThreeMonthCurrentOutflowVsAveragePercent { get; private set; }
    public decimal? ThreeMonthCurrentNetFlowVsAverageDifference { get; private set; }

    public decimal? ThreeMonthCurrentInflowVsHighPercent { get; private set; }
    public decimal? ThreeMonthCurrentOutflowVsHighPercent { get; private set; }
    public decimal? ThreeMonthCurrentNetFlowVsHighDifference { get; private set; }

    public decimal? ThreeMonthCurrentInflowVsLowPercent { get; private set; }
    public decimal? ThreeMonthCurrentOutflowVsLowPercent { get; private set; }
    public decimal? ThreeMonthCurrentNetFlowVsLowDifference { get; private set; }

    // Historical 6M - excludes current month
    public decimal? SixMonthInflowAverage { get; private set; }
    public decimal? SixMonthInflowHigh { get; private set; }
    public string? SixMonthInflowHighPeriod { get; private set; }
    public decimal? SixMonthInflowLow { get; private set; }
    public string? SixMonthInflowLowPeriod { get; private set; }

    public decimal? SixMonthOutflowAverage { get; private set; }
    public decimal? SixMonthOutflowHigh { get; private set; }
    public string? SixMonthOutflowHighPeriod { get; private set; }
    public decimal? SixMonthOutflowLow { get; private set; }
    public string? SixMonthOutflowLowPeriod { get; private set; }

    public decimal? SixMonthNetFlowAverage { get; private set; }
    public decimal? SixMonthNetFlowHigh { get; private set; }
    public string? SixMonthNetFlowHighPeriod { get; private set; }
    public decimal? SixMonthNetFlowLow { get; private set; }
    public string? SixMonthNetFlowLowPeriod { get; private set; }

    public decimal? SixMonthCurrentInflowVsAveragePercent { get; private set; }
    public decimal? SixMonthCurrentOutflowVsAveragePercent { get; private set; }
    public decimal? SixMonthCurrentNetFlowVsAverageDifference { get; private set; }

    public decimal? SixMonthCurrentInflowVsHighPercent { get; private set; }
    public decimal? SixMonthCurrentOutflowVsHighPercent { get; private set; }
    public decimal? SixMonthCurrentNetFlowVsHighDifference { get; private set; }

    public decimal? SixMonthCurrentInflowVsLowPercent { get; private set; }
    public decimal? SixMonthCurrentOutflowVsLowPercent { get; private set; }
    public decimal? SixMonthCurrentNetFlowVsLowDifference { get; private set; }

    // Historical 12M - excludes current month
    public decimal? TwelveMonthInflowAverage { get; private set; }
    public decimal? TwelveMonthInflowHigh { get; private set; }
    public string? TwelveMonthInflowHighPeriod { get; private set; }
    public decimal? TwelveMonthInflowLow { get; private set; }
    public string? TwelveMonthInflowLowPeriod { get; private set; }

    public decimal? TwelveMonthOutflowAverage { get; private set; }
    public decimal? TwelveMonthOutflowHigh { get; private set; }
    public string? TwelveMonthOutflowHighPeriod { get; private set; }
    public decimal? TwelveMonthOutflowLow { get; private set; }
    public string? TwelveMonthOutflowLowPeriod { get; private set; }

    public decimal? TwelveMonthNetFlowAverage { get; private set; }
    public decimal? TwelveMonthNetFlowHigh { get; private set; }
    public string? TwelveMonthNetFlowHighPeriod { get; private set; }
    public decimal? TwelveMonthNetFlowLow { get; private set; }
    public string? TwelveMonthNetFlowLowPeriod { get; private set; }

    public decimal? TwelveMonthCurrentInflowVsAveragePercent { get; private set; }
    public decimal? TwelveMonthCurrentOutflowVsAveragePercent { get; private set; }
    public decimal? TwelveMonthCurrentNetFlowVsAverageDifference { get; private set; }

    public decimal? TwelveMonthCurrentInflowVsHighPercent { get; private set; }
    public decimal? TwelveMonthCurrentOutflowVsHighPercent { get; private set; }
    public decimal? TwelveMonthCurrentNetFlowVsHighDifference { get; private set; }

    public decimal? TwelveMonthCurrentInflowVsLowPercent { get; private set; }
    public decimal? TwelveMonthCurrentOutflowVsLowPercent { get; private set; }
    public decimal? TwelveMonthCurrentNetFlowVsLowDifference { get; private set; }

    // Historical 24M - excludes current month
    public decimal? TwentyFourMonthInflowAverage { get; private set; }
    public decimal? TwentyFourMonthInflowHigh { get; private set; }
    public string? TwentyFourMonthInflowHighPeriod { get; private set; }
    public decimal? TwentyFourMonthInflowLow { get; private set; }
    public string? TwentyFourMonthInflowLowPeriod { get; private set; }

    public decimal? TwentyFourMonthOutflowAverage { get; private set; }
    public decimal? TwentyFourMonthOutflowHigh { get; private set; }
    public string? TwentyFourMonthOutflowHighPeriod { get; private set; }
    public decimal? TwentyFourMonthOutflowLow { get; private set; }
    public string? TwentyFourMonthOutflowLowPeriod { get; private set; }

    public decimal? TwentyFourMonthNetFlowAverage { get; private set; }
    public decimal? TwentyFourMonthNetFlowHigh { get; private set; }
    public string? TwentyFourMonthNetFlowHighPeriod { get; private set; }
    public decimal? TwentyFourMonthNetFlowLow { get; private set; }
    public string? TwentyFourMonthNetFlowLowPeriod { get; private set; }

    public decimal? TwentyFourMonthCurrentInflowVsAveragePercent { get; private set; }
    public decimal? TwentyFourMonthCurrentOutflowVsAveragePercent { get; private set; }
    public decimal? TwentyFourMonthCurrentNetFlowVsAverageDifference { get; private set; }

    public decimal? TwentyFourMonthCurrentInflowVsHighPercent { get; private set; }
    public decimal? TwentyFourMonthCurrentOutflowVsHighPercent { get; private set; }
    public decimal? TwentyFourMonthCurrentNetFlowVsHighDifference { get; private set; }

    public decimal? TwentyFourMonthCurrentInflowVsLowPercent { get; private set; }
    public decimal? TwentyFourMonthCurrentOutflowVsLowPercent { get; private set; }
    public decimal? TwentyFourMonthCurrentNetFlowVsLowDifference { get; private set; }

    // Historical 60M - excludes current month
    public decimal? SixtyMonthInflowAverage { get; private set; }
    public decimal? SixtyMonthInflowHigh { get; private set; }
    public string? SixtyMonthInflowHighPeriod { get; private set; }
    public decimal? SixtyMonthInflowLow { get; private set; }
    public string? SixtyMonthInflowLowPeriod { get; private set; }

    public decimal? SixtyMonthOutflowAverage { get; private set; }
    public decimal? SixtyMonthOutflowHigh { get; private set; }
    public string? SixtyMonthOutflowHighPeriod { get; private set; }
    public decimal? SixtyMonthOutflowLow { get; private set; }
    public string? SixtyMonthOutflowLowPeriod { get; private set; }

    public decimal? SixtyMonthNetFlowAverage { get; private set; }
    public decimal? SixtyMonthNetFlowHigh { get; private set; }
    public string? SixtyMonthNetFlowHighPeriod { get; private set; }
    public decimal? SixtyMonthNetFlowLow { get; private set; }
    public string? SixtyMonthNetFlowLowPeriod { get; private set; }

    public decimal? SixtyMonthCurrentInflowVsAveragePercent { get; private set; }
    public decimal? SixtyMonthCurrentOutflowVsAveragePercent { get; private set; }
    public decimal? SixtyMonthCurrentNetFlowVsAverageDifference { get; private set; }

    public decimal? SixtyMonthCurrentInflowVsHighPercent { get; private set; }
    public decimal? SixtyMonthCurrentOutflowVsHighPercent { get; private set; }
    public decimal? SixtyMonthCurrentNetFlowVsHighDifference { get; private set; }

    public decimal? SixtyMonthCurrentInflowVsLowPercent { get; private set; }
    public decimal? SixtyMonthCurrentOutflowVsLowPercent { get; private set; }
    public decimal? SixtyMonthCurrentNetFlowVsLowDifference { get; private set; }

    // Data coverage
    public int CurrentMonthTradingDays { get; private set; }
    public int CurrentMonthValidFlowDays { get; private set; }
    public int CurrentMonthInflowDays { get; private set; }
    public int CurrentMonthOutflowDays { get; private set; }

    public decimal CurrentMonthTradeValue { get; private set; }
    public decimal? CurrentMonthNetFlowToTradeValuePercent { get; private set; }

    public bool HasComparablePreviousYearData { get; private set; }
    public bool HasComparableHistoricalBaseline { get; private set; }

    public int AvailableHistoryMonths { get; private set; }
    public DateOnly LastFlowTradeDate { get; private set; }
    public DateTimeOffset CalculatedAt { get; private set; }

    public void Update(
        string symbol,
        RealMoneyFlowSnapshotMetrics metrics)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        Symbol = symbol;
        Apply(metrics);
    }

    private void Apply(RealMoneyFlowSnapshotMetrics metrics)
    {
        ArgumentNullException.ThrowIfNull(metrics);

        if (metrics.CurrentMonth is null ||
            metrics.LastFlowTradeDate == default ||
            metrics.CurrentMonthTradingDays < 1 ||
            metrics.CurrentMonthValidFlowDays < 1 ||
            metrics.CurrentMonthValidFlowDays > metrics.CurrentMonthTradingDays ||
            metrics.CurrentMonthInflowDays < 0 ||
            metrics.CurrentMonthOutflowDays < 0 ||
            metrics.CurrentMonthInflowDays +
                metrics.CurrentMonthOutflowDays > metrics.CurrentMonthValidFlowDays ||
            metrics.CurrentMonthTradeValue < 0 ||
            metrics.AvailableHistoryMonths < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(metrics));
        }

        ValidateAmounts(metrics.CurrentMonth);
        ValidateAmounts(metrics.PreviousYearCurrentMonth);
        ValidateAmounts(metrics.RollingThreeMonth);
        ValidateAmounts(metrics.PreviousYearRollingThreeMonth);
        ValidateAmounts(metrics.RollingSixMonth);
        ValidateAmounts(metrics.PreviousYearRollingSixMonth);
        ValidateAmounts(metrics.YearToDate);
        ValidateAmounts(metrics.PreviousYearToDate);

        ValidateHistorical(metrics.ThreeMonthHistory);
        ValidateHistorical(metrics.SixMonthHistory);
        ValidateHistorical(metrics.TwelveMonthHistory);
        ValidateHistorical(metrics.TwentyFourMonthHistory);
        ValidateHistorical(metrics.SixtyMonthHistory);

        CurrentMonthInflow = metrics.CurrentMonth.Inflow;
        CurrentMonthOutflow = metrics.CurrentMonth.Outflow;
        CurrentMonthNetFlow = metrics.CurrentMonth.NetFlow;

        PreviousYearCurrentMonthInflow = metrics.PreviousYearCurrentMonth?.Inflow;
        PreviousYearCurrentMonthOutflow = metrics.PreviousYearCurrentMonth?.Outflow;
        PreviousYearCurrentMonthNetFlow = metrics.PreviousYearCurrentMonth?.NetFlow;

        RollingThreeMonthInflow = metrics.RollingThreeMonth?.Inflow;
        RollingThreeMonthOutflow = metrics.RollingThreeMonth?.Outflow;
        RollingThreeMonthNetFlow = metrics.RollingThreeMonth?.NetFlow;

        PreviousYearRollingThreeMonthInflow =
            metrics.PreviousYearRollingThreeMonth?.Inflow;
        PreviousYearRollingThreeMonthOutflow =
            metrics.PreviousYearRollingThreeMonth?.Outflow;
        PreviousYearRollingThreeMonthNetFlow =
            metrics.PreviousYearRollingThreeMonth?.NetFlow;

        RollingSixMonthInflow = metrics.RollingSixMonth?.Inflow;
        RollingSixMonthOutflow = metrics.RollingSixMonth?.Outflow;
        RollingSixMonthNetFlow = metrics.RollingSixMonth?.NetFlow;

        PreviousYearRollingSixMonthInflow =
            metrics.PreviousYearRollingSixMonth?.Inflow;
        PreviousYearRollingSixMonthOutflow =
            metrics.PreviousYearRollingSixMonth?.Outflow;
        PreviousYearRollingSixMonthNetFlow =
            metrics.PreviousYearRollingSixMonth?.NetFlow;

        YearToDateInflow = metrics.YearToDate?.Inflow;
        YearToDateOutflow = metrics.YearToDate?.Outflow;
        YearToDateNetFlow = metrics.YearToDate?.NetFlow;

        PreviousYearToDateInflow = metrics.PreviousYearToDate?.Inflow;
        PreviousYearToDateOutflow = metrics.PreviousYearToDate?.Outflow;
        PreviousYearToDateNetFlow = metrics.PreviousYearToDate?.NetFlow;

        MonthlyInflowYoYGrowthPercent =
            metrics.HasComparablePreviousYearData
                ? YoYPercent(CurrentMonthInflow, PreviousYearCurrentMonthInflow)
                : null;

        MonthlyOutflowYoYGrowthPercent =
            metrics.HasComparablePreviousYearData
                ? YoYPercent(CurrentMonthOutflow, PreviousYearCurrentMonthOutflow)
                : null;

        MonthlyNetFlowYoYChange =
            metrics.HasComparablePreviousYearData
                ? Difference(CurrentMonthNetFlow, PreviousYearCurrentMonthNetFlow)
                : null;

        ThreeMonthInflowYoYGrowthPercent =
            metrics.HasComparablePreviousYearData
                ? YoYPercent(
                    RollingThreeMonthInflow,
                    PreviousYearRollingThreeMonthInflow)
                : null;

        ThreeMonthOutflowYoYGrowthPercent =
            metrics.HasComparablePreviousYearData
                ? YoYPercent(
                    RollingThreeMonthOutflow,
                    PreviousYearRollingThreeMonthOutflow)
                : null;

        ThreeMonthNetFlowYoYChange =
            metrics.HasComparablePreviousYearData
                ? Difference(
                    RollingThreeMonthNetFlow,
                    PreviousYearRollingThreeMonthNetFlow)
                : null;

        SixMonthInflowYoYGrowthPercent =
            metrics.HasComparablePreviousYearData
                ? YoYPercent(
                    RollingSixMonthInflow,
                    PreviousYearRollingSixMonthInflow)
                : null;

        SixMonthOutflowYoYGrowthPercent =
            metrics.HasComparablePreviousYearData
                ? YoYPercent(
                    RollingSixMonthOutflow,
                    PreviousYearRollingSixMonthOutflow)
                : null;

        SixMonthNetFlowYoYChange =
            metrics.HasComparablePreviousYearData
                ? Difference(
                    RollingSixMonthNetFlow,
                    PreviousYearRollingSixMonthNetFlow)
                : null;

        YearToDateInflowYoYGrowthPercent =
            metrics.HasComparablePreviousYearData
                ? YoYPercent(YearToDateInflow, PreviousYearToDateInflow)
                : null;

        YearToDateOutflowYoYGrowthPercent =
            metrics.HasComparablePreviousYearData
                ? YoYPercent(YearToDateOutflow, PreviousYearToDateOutflow)
                : null;

        YearToDateNetFlowYoYChange =
            metrics.HasComparablePreviousYearData
                ? Difference(YearToDateNetFlow, PreviousYearToDateNetFlow)
                : null;

        ApplyThreeMonthHistory(metrics);
        ApplySixMonthHistory(metrics);
        ApplyTwelveMonthHistory(metrics);
        ApplyTwentyFourMonthHistory(metrics);
        ApplySixtyMonthHistory(metrics);

        CurrentMonthTradingDays = metrics.CurrentMonthTradingDays;
        CurrentMonthValidFlowDays = metrics.CurrentMonthValidFlowDays;
        CurrentMonthInflowDays = metrics.CurrentMonthInflowDays;
        CurrentMonthOutflowDays = metrics.CurrentMonthOutflowDays;
        CurrentMonthTradeValue = metrics.CurrentMonthTradeValue;

        CurrentMonthNetFlowToTradeValuePercent =
            metrics.CurrentMonthTradeValue > 0
                ? Math.Round(
                    CurrentMonthNetFlow.Value /
                    metrics.CurrentMonthTradeValue * 100m, 4)
                : null;

        HasComparablePreviousYearData =
            metrics.HasComparablePreviousYearData;

        HasComparableHistoricalBaseline =
            metrics.HasComparableHistoricalBaseline;

        AvailableHistoryMonths = metrics.AvailableHistoryMonths;
        LastFlowTradeDate = metrics.LastFlowTradeDate;
        CalculatedAt = DateTimeOffset.UtcNow;
    }

    private void ApplyThreeMonthHistory(RealMoneyFlowSnapshotMetrics metrics)
    {
        ThreeMonthInflowAverage = metrics.ThreeMonthHistory?.Inflow.Average;
        ThreeMonthInflowHigh = metrics.ThreeMonthHistory?.Inflow.High;
        ThreeMonthInflowHighPeriod = metrics.ThreeMonthHistory?.Inflow.HighPeriod;
        ThreeMonthInflowLow = metrics.ThreeMonthHistory?.Inflow.Low;
        ThreeMonthInflowLowPeriod = metrics.ThreeMonthHistory?.Inflow.LowPeriod;

        ThreeMonthOutflowAverage = metrics.ThreeMonthHistory?.Outflow.Average;
        ThreeMonthOutflowHigh = metrics.ThreeMonthHistory?.Outflow.High;
        ThreeMonthOutflowHighPeriod = metrics.ThreeMonthHistory?.Outflow.HighPeriod;
        ThreeMonthOutflowLow = metrics.ThreeMonthHistory?.Outflow.Low;
        ThreeMonthOutflowLowPeriod = metrics.ThreeMonthHistory?.Outflow.LowPeriod;

        ThreeMonthNetFlowAverage = metrics.ThreeMonthHistory?.NetFlow.Average;
        ThreeMonthNetFlowHigh = metrics.ThreeMonthHistory?.NetFlow.High;
        ThreeMonthNetFlowHighPeriod = metrics.ThreeMonthHistory?.NetFlow.HighPeriod;
        ThreeMonthNetFlowLow = metrics.ThreeMonthHistory?.NetFlow.Low;
        ThreeMonthNetFlowLowPeriod = metrics.ThreeMonthHistory?.NetFlow.LowPeriod;

        ThreeMonthCurrentInflowVsAveragePercent =
            metrics.HasComparableHistoricalBaseline
                ? VsPercent(CurrentMonthInflow, ThreeMonthInflowAverage)
                : null;

        ThreeMonthCurrentOutflowVsAveragePercent =
            metrics.HasComparableHistoricalBaseline
                ? VsPercent(CurrentMonthOutflow, ThreeMonthOutflowAverage)
                : null;

        ThreeMonthCurrentNetFlowVsAverageDifference =
            metrics.HasComparableHistoricalBaseline
                ? Difference(CurrentMonthNetFlow, ThreeMonthNetFlowAverage)
                : null;

        ThreeMonthCurrentInflowVsHighPercent =
            metrics.HasComparableHistoricalBaseline
                ? VsPercent(CurrentMonthInflow, ThreeMonthInflowHigh)
                : null;

        ThreeMonthCurrentOutflowVsHighPercent =
            metrics.HasComparableHistoricalBaseline
                ? VsPercent(CurrentMonthOutflow, ThreeMonthOutflowHigh)
                : null;

        ThreeMonthCurrentNetFlowVsHighDifference =
            metrics.HasComparableHistoricalBaseline
                ? Difference(CurrentMonthNetFlow, ThreeMonthNetFlowHigh)
                : null;

        ThreeMonthCurrentInflowVsLowPercent =
            metrics.HasComparableHistoricalBaseline
                ? VsPercent(CurrentMonthInflow, ThreeMonthInflowLow)
                : null;

        ThreeMonthCurrentOutflowVsLowPercent =
            metrics.HasComparableHistoricalBaseline
                ? VsPercent(CurrentMonthOutflow, ThreeMonthOutflowLow)
                : null;

        ThreeMonthCurrentNetFlowVsLowDifference =
            metrics.HasComparableHistoricalBaseline
                ? Difference(CurrentMonthNetFlow, ThreeMonthNetFlowLow)
                : null;
    }

    private void ApplySixMonthHistory(RealMoneyFlowSnapshotMetrics metrics)
    {
        SixMonthInflowAverage = metrics.SixMonthHistory?.Inflow.Average;
        SixMonthInflowHigh = metrics.SixMonthHistory?.Inflow.High;
        SixMonthInflowHighPeriod = metrics.SixMonthHistory?.Inflow.HighPeriod;
        SixMonthInflowLow = metrics.SixMonthHistory?.Inflow.Low;
        SixMonthInflowLowPeriod = metrics.SixMonthHistory?.Inflow.LowPeriod;

        SixMonthOutflowAverage = metrics.SixMonthHistory?.Outflow.Average;
        SixMonthOutflowHigh = metrics.SixMonthHistory?.Outflow.High;
        SixMonthOutflowHighPeriod = metrics.SixMonthHistory?.Outflow.HighPeriod;
        SixMonthOutflowLow = metrics.SixMonthHistory?.Outflow.Low;
        SixMonthOutflowLowPeriod = metrics.SixMonthHistory?.Outflow.LowPeriod;

        SixMonthNetFlowAverage = metrics.SixMonthHistory?.NetFlow.Average;
        SixMonthNetFlowHigh = metrics.SixMonthHistory?.NetFlow.High;
        SixMonthNetFlowHighPeriod = metrics.SixMonthHistory?.NetFlow.HighPeriod;
        SixMonthNetFlowLow = metrics.SixMonthHistory?.NetFlow.Low;
        SixMonthNetFlowLowPeriod = metrics.SixMonthHistory?.NetFlow.LowPeriod;

        SixMonthCurrentInflowVsAveragePercent =
            metrics.HasComparableHistoricalBaseline
                ? VsPercent(CurrentMonthInflow, SixMonthInflowAverage)
                : null;

        SixMonthCurrentOutflowVsAveragePercent =
            metrics.HasComparableHistoricalBaseline
                ? VsPercent(CurrentMonthOutflow, SixMonthOutflowAverage)
                : null;

        SixMonthCurrentNetFlowVsAverageDifference =
            metrics.HasComparableHistoricalBaseline
                ? Difference(CurrentMonthNetFlow, SixMonthNetFlowAverage)
                : null;

        SixMonthCurrentInflowVsHighPercent =
            metrics.HasComparableHistoricalBaseline
                ? VsPercent(CurrentMonthInflow, SixMonthInflowHigh)
                : null;

        SixMonthCurrentOutflowVsHighPercent =
            metrics.HasComparableHistoricalBaseline
                ? VsPercent(CurrentMonthOutflow, SixMonthOutflowHigh)
                : null;

        SixMonthCurrentNetFlowVsHighDifference =
            metrics.HasComparableHistoricalBaseline
                ? Difference(CurrentMonthNetFlow, SixMonthNetFlowHigh)
                : null;

        SixMonthCurrentInflowVsLowPercent =
            metrics.HasComparableHistoricalBaseline
                ? VsPercent(CurrentMonthInflow, SixMonthInflowLow)
                : null;

        SixMonthCurrentOutflowVsLowPercent =
            metrics.HasComparableHistoricalBaseline
                ? VsPercent(CurrentMonthOutflow, SixMonthOutflowLow)
                : null;

        SixMonthCurrentNetFlowVsLowDifference =
            metrics.HasComparableHistoricalBaseline
                ? Difference(CurrentMonthNetFlow, SixMonthNetFlowLow)
                : null;
    }

    private void ApplyTwelveMonthHistory(RealMoneyFlowSnapshotMetrics metrics)
    {
        TwelveMonthInflowAverage = metrics.TwelveMonthHistory?.Inflow.Average;
        TwelveMonthInflowHigh = metrics.TwelveMonthHistory?.Inflow.High;
        TwelveMonthInflowHighPeriod = metrics.TwelveMonthHistory?.Inflow.HighPeriod;
        TwelveMonthInflowLow = metrics.TwelveMonthHistory?.Inflow.Low;
        TwelveMonthInflowLowPeriod = metrics.TwelveMonthHistory?.Inflow.LowPeriod;

        TwelveMonthOutflowAverage = metrics.TwelveMonthHistory?.Outflow.Average;
        TwelveMonthOutflowHigh = metrics.TwelveMonthHistory?.Outflow.High;
        TwelveMonthOutflowHighPeriod = metrics.TwelveMonthHistory?.Outflow.HighPeriod;
        TwelveMonthOutflowLow = metrics.TwelveMonthHistory?.Outflow.Low;
        TwelveMonthOutflowLowPeriod = metrics.TwelveMonthHistory?.Outflow.LowPeriod;

        TwelveMonthNetFlowAverage = metrics.TwelveMonthHistory?.NetFlow.Average;
        TwelveMonthNetFlowHigh = metrics.TwelveMonthHistory?.NetFlow.High;
        TwelveMonthNetFlowHighPeriod = metrics.TwelveMonthHistory?.NetFlow.HighPeriod;
        TwelveMonthNetFlowLow = metrics.TwelveMonthHistory?.NetFlow.Low;
        TwelveMonthNetFlowLowPeriod = metrics.TwelveMonthHistory?.NetFlow.LowPeriod;

        TwelveMonthCurrentInflowVsAveragePercent =
            metrics.HasComparableHistoricalBaseline
                ? VsPercent(CurrentMonthInflow, TwelveMonthInflowAverage)
                : null;

        TwelveMonthCurrentOutflowVsAveragePercent =
            metrics.HasComparableHistoricalBaseline
                ? VsPercent(CurrentMonthOutflow, TwelveMonthOutflowAverage)
                : null;

        TwelveMonthCurrentNetFlowVsAverageDifference =
            metrics.HasComparableHistoricalBaseline
                ? Difference(CurrentMonthNetFlow, TwelveMonthNetFlowAverage)
                : null;

        TwelveMonthCurrentInflowVsHighPercent =
            metrics.HasComparableHistoricalBaseline
                ? VsPercent(CurrentMonthInflow, TwelveMonthInflowHigh)
                : null;

        TwelveMonthCurrentOutflowVsHighPercent =
            metrics.HasComparableHistoricalBaseline
                ? VsPercent(CurrentMonthOutflow, TwelveMonthOutflowHigh)
                : null;

        TwelveMonthCurrentNetFlowVsHighDifference =
            metrics.HasComparableHistoricalBaseline
                ? Difference(CurrentMonthNetFlow, TwelveMonthNetFlowHigh)
                : null;

        TwelveMonthCurrentInflowVsLowPercent =
            metrics.HasComparableHistoricalBaseline
                ? VsPercent(CurrentMonthInflow, TwelveMonthInflowLow)
                : null;

        TwelveMonthCurrentOutflowVsLowPercent =
            metrics.HasComparableHistoricalBaseline
                ? VsPercent(CurrentMonthOutflow, TwelveMonthOutflowLow)
                : null;

        TwelveMonthCurrentNetFlowVsLowDifference =
            metrics.HasComparableHistoricalBaseline
                ? Difference(CurrentMonthNetFlow, TwelveMonthNetFlowLow)
                : null;
    }

    private void ApplyTwentyFourMonthHistory(RealMoneyFlowSnapshotMetrics metrics)
    {
        TwentyFourMonthInflowAverage = metrics.TwentyFourMonthHistory?.Inflow.Average;
        TwentyFourMonthInflowHigh = metrics.TwentyFourMonthHistory?.Inflow.High;
        TwentyFourMonthInflowHighPeriod = metrics.TwentyFourMonthHistory?.Inflow.HighPeriod;
        TwentyFourMonthInflowLow = metrics.TwentyFourMonthHistory?.Inflow.Low;
        TwentyFourMonthInflowLowPeriod = metrics.TwentyFourMonthHistory?.Inflow.LowPeriod;

        TwentyFourMonthOutflowAverage = metrics.TwentyFourMonthHistory?.Outflow.Average;
        TwentyFourMonthOutflowHigh = metrics.TwentyFourMonthHistory?.Outflow.High;
        TwentyFourMonthOutflowHighPeriod = metrics.TwentyFourMonthHistory?.Outflow.HighPeriod;
        TwentyFourMonthOutflowLow = metrics.TwentyFourMonthHistory?.Outflow.Low;
        TwentyFourMonthOutflowLowPeriod = metrics.TwentyFourMonthHistory?.Outflow.LowPeriod;

        TwentyFourMonthNetFlowAverage = metrics.TwentyFourMonthHistory?.NetFlow.Average;
        TwentyFourMonthNetFlowHigh = metrics.TwentyFourMonthHistory?.NetFlow.High;
        TwentyFourMonthNetFlowHighPeriod = metrics.TwentyFourMonthHistory?.NetFlow.HighPeriod;
        TwentyFourMonthNetFlowLow = metrics.TwentyFourMonthHistory?.NetFlow.Low;
        TwentyFourMonthNetFlowLowPeriod = metrics.TwentyFourMonthHistory?.NetFlow.LowPeriod;

        TwentyFourMonthCurrentInflowVsAveragePercent =
            metrics.HasComparableHistoricalBaseline
                ? VsPercent(CurrentMonthInflow, TwentyFourMonthInflowAverage)
                : null;

        TwentyFourMonthCurrentOutflowVsAveragePercent =
            metrics.HasComparableHistoricalBaseline
                ? VsPercent(CurrentMonthOutflow, TwentyFourMonthOutflowAverage)
                : null;

        TwentyFourMonthCurrentNetFlowVsAverageDifference =
            metrics.HasComparableHistoricalBaseline
                ? Difference(CurrentMonthNetFlow, TwentyFourMonthNetFlowAverage)
                : null;

        TwentyFourMonthCurrentInflowVsHighPercent =
            metrics.HasComparableHistoricalBaseline
                ? VsPercent(CurrentMonthInflow, TwentyFourMonthInflowHigh)
                : null;

        TwentyFourMonthCurrentOutflowVsHighPercent =
            metrics.HasComparableHistoricalBaseline
                ? VsPercent(CurrentMonthOutflow, TwentyFourMonthOutflowHigh)
                : null;

        TwentyFourMonthCurrentNetFlowVsHighDifference =
            metrics.HasComparableHistoricalBaseline
                ? Difference(CurrentMonthNetFlow, TwentyFourMonthNetFlowHigh)
                : null;

        TwentyFourMonthCurrentInflowVsLowPercent =
            metrics.HasComparableHistoricalBaseline
                ? VsPercent(CurrentMonthInflow, TwentyFourMonthInflowLow)
                : null;

        TwentyFourMonthCurrentOutflowVsLowPercent =
            metrics.HasComparableHistoricalBaseline
                ? VsPercent(CurrentMonthOutflow, TwentyFourMonthOutflowLow)
                : null;

        TwentyFourMonthCurrentNetFlowVsLowDifference =
            metrics.HasComparableHistoricalBaseline
                ? Difference(CurrentMonthNetFlow, TwentyFourMonthNetFlowLow)
                : null;
    }

    private void ApplySixtyMonthHistory(RealMoneyFlowSnapshotMetrics metrics)
    {
        SixtyMonthInflowAverage = metrics.SixtyMonthHistory?.Inflow.Average;
        SixtyMonthInflowHigh = metrics.SixtyMonthHistory?.Inflow.High;
        SixtyMonthInflowHighPeriod = metrics.SixtyMonthHistory?.Inflow.HighPeriod;
        SixtyMonthInflowLow = metrics.SixtyMonthHistory?.Inflow.Low;
        SixtyMonthInflowLowPeriod = metrics.SixtyMonthHistory?.Inflow.LowPeriod;

        SixtyMonthOutflowAverage = metrics.SixtyMonthHistory?.Outflow.Average;
        SixtyMonthOutflowHigh = metrics.SixtyMonthHistory?.Outflow.High;
        SixtyMonthOutflowHighPeriod = metrics.SixtyMonthHistory?.Outflow.HighPeriod;
        SixtyMonthOutflowLow = metrics.SixtyMonthHistory?.Outflow.Low;
        SixtyMonthOutflowLowPeriod = metrics.SixtyMonthHistory?.Outflow.LowPeriod;

        SixtyMonthNetFlowAverage = metrics.SixtyMonthHistory?.NetFlow.Average;
        SixtyMonthNetFlowHigh = metrics.SixtyMonthHistory?.NetFlow.High;
        SixtyMonthNetFlowHighPeriod = metrics.SixtyMonthHistory?.NetFlow.HighPeriod;
        SixtyMonthNetFlowLow = metrics.SixtyMonthHistory?.NetFlow.Low;
        SixtyMonthNetFlowLowPeriod = metrics.SixtyMonthHistory?.NetFlow.LowPeriod;

        SixtyMonthCurrentInflowVsAveragePercent =
            metrics.HasComparableHistoricalBaseline
                ? VsPercent(CurrentMonthInflow, SixtyMonthInflowAverage)
                : null;

        SixtyMonthCurrentOutflowVsAveragePercent =
            metrics.HasComparableHistoricalBaseline
                ? VsPercent(CurrentMonthOutflow, SixtyMonthOutflowAverage)
                : null;

        SixtyMonthCurrentNetFlowVsAverageDifference =
            metrics.HasComparableHistoricalBaseline
                ? Difference(CurrentMonthNetFlow, SixtyMonthNetFlowAverage)
                : null;

        SixtyMonthCurrentInflowVsHighPercent =
            metrics.HasComparableHistoricalBaseline
                ? VsPercent(CurrentMonthInflow, SixtyMonthInflowHigh)
                : null;

        SixtyMonthCurrentOutflowVsHighPercent =
            metrics.HasComparableHistoricalBaseline
                ? VsPercent(CurrentMonthOutflow, SixtyMonthOutflowHigh)
                : null;

        SixtyMonthCurrentNetFlowVsHighDifference =
            metrics.HasComparableHistoricalBaseline
                ? Difference(CurrentMonthNetFlow, SixtyMonthNetFlowHigh)
                : null;

        SixtyMonthCurrentInflowVsLowPercent =
            metrics.HasComparableHistoricalBaseline
                ? VsPercent(CurrentMonthInflow, SixtyMonthInflowLow)
                : null;

        SixtyMonthCurrentOutflowVsLowPercent =
            metrics.HasComparableHistoricalBaseline
                ? VsPercent(CurrentMonthOutflow, SixtyMonthOutflowLow)
                : null;

        SixtyMonthCurrentNetFlowVsLowDifference =
            metrics.HasComparableHistoricalBaseline
                ? Difference(CurrentMonthNetFlow, SixtyMonthNetFlowLow)
                : null;
    }

    private static void ValidateAmounts(RealMoneyFlowAmounts? amounts)
    {
        if (amounts is not null &&
            (amounts.Inflow < 0 || amounts.Outflow < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(amounts));
        }
    }

    private static void ValidateHistorical(
        RealMoneyFlowHistoricalMetrics? history)
    {
        if (history is null)
            return;

        ValidateHistoricalSeries(history.Inflow, false);
        ValidateHistoricalSeries(history.Outflow, false);
        ValidateHistoricalSeries(history.NetFlow, true);
    }

    private static void ValidateHistoricalSeries(
        RealMoneyFlowHistoricalSeries series,
        bool canBeNegative)
    {
        ArgumentNullException.ThrowIfNull(series);

        if ((series.High.HasValue &&
             series.Low.HasValue &&
             series.High < series.Low) ||
            (!canBeNegative &&
             (series.Average < 0 ||
              series.High < 0 ||
              series.Low < 0)))
        {
            throw new ArgumentOutOfRangeException(nameof(series));
        }
    }

    private static decimal? YoYPercent(
        decimal? current,
        decimal? previous)
    {
        return current.HasValue && previous is > 0m
            ? Math.Round(
                (current.Value - previous.Value) /
                previous.Value * 100m, 4)
            : null;
    }

    private static decimal? VsPercent(
        decimal? current,
        decimal? historical)
    {
        return current.HasValue && historical is > 0m
            ? Math.Round(
                (current.Value - historical.Value) /
                historical.Value * 100m, 4)
            : null;
    }

    private static decimal? Difference(
        decimal? current,
        decimal? previous)
    {
        return current.HasValue && previous.HasValue
            ? current.Value - previous.Value
            : null;
    }
}

public sealed record RealMoneyFlowAmounts(
    decimal Inflow,
    decimal Outflow)
{
    public decimal NetFlow => Inflow - Outflow;
}

public sealed record RealMoneyFlowHistoricalSeries(
    decimal? Average,
    decimal? High,
    string? HighPeriod,
    decimal? Low,
    string? LowPeriod);

public sealed record RealMoneyFlowHistoricalMetrics(
    RealMoneyFlowHistoricalSeries Inflow,
    RealMoneyFlowHistoricalSeries Outflow,
    RealMoneyFlowHistoricalSeries NetFlow);

public sealed record RealMoneyFlowSnapshotMetrics
{
    public required RealMoneyFlowAmounts CurrentMonth { get; init; }

    public RealMoneyFlowAmounts? PreviousYearCurrentMonth { get; init; }
    public RealMoneyFlowAmounts? RollingThreeMonth { get; init; }
    public RealMoneyFlowAmounts? PreviousYearRollingThreeMonth { get; init; }
    public RealMoneyFlowAmounts? RollingSixMonth { get; init; }
    public RealMoneyFlowAmounts? PreviousYearRollingSixMonth { get; init; }

    public RealMoneyFlowAmounts? YearToDate { get; init; }
    public RealMoneyFlowAmounts? PreviousYearToDate { get; init; }

    public RealMoneyFlowHistoricalMetrics? ThreeMonthHistory { get; init; }
    public RealMoneyFlowHistoricalMetrics? SixMonthHistory { get; init; }
    public RealMoneyFlowHistoricalMetrics? TwelveMonthHistory { get; init; }
    public RealMoneyFlowHistoricalMetrics? TwentyFourMonthHistory { get; init; }
    public RealMoneyFlowHistoricalMetrics? SixtyMonthHistory { get; init; }

    public int CurrentMonthTradingDays { get; init; }
    public int CurrentMonthValidFlowDays { get; init; }
    public int CurrentMonthInflowDays { get; init; }
    public int CurrentMonthOutflowDays { get; init; }

    public decimal CurrentMonthTradeValue { get; init; }

    public bool HasComparablePreviousYearData { get; init; }
    public bool HasComparableHistoricalBaseline { get; init; }

    public int AvailableHistoryMonths { get; init; }
    public DateOnly LastFlowTradeDate { get; init; }
}
