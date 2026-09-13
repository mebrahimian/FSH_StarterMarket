using FSH.Framework.Core.Domain;
namespace FSH.Modules.MarketIntelligence.Domain.Insights;
public sealed class SalesPerformanceSnapshot : BaseEntity<Guid>, IGlobalEntity
{
    private SalesPerformanceSnapshot()
    {
    }
    public SalesPerformanceSnapshot(
        int companyId,
        string symbol,
        string periodEndDate,
        SalesPerformanceMetrics metrics)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(companyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(periodEndDate);
        ArgumentNullException.ThrowIfNull(metrics);

        CompanyId = companyId;
        Symbol = symbol;
        PeriodEndDate = periodEndDate;
        Apply(metrics);
    }
    public int CompanyId { get; private set; }
    public string Symbol { get; private set; } = string.Empty;
    public string PeriodEndDate { get; private set; } = string.Empty;
    // ─────────────────────────────────────────────
    // Current month / 1M
    // ─────────────────────────────────────────────
    public decimal? CurrentAmount { get; private set; }
    public decimal? PreviousYearCurrentAmount { get; private set; }
    public decimal? MonthlyYoYGrowthPercent { get; private set; }
    // ─────────────────────────────────────────────
    // Rolling 3M
    // شامل ماه جاری
    // ─────────────────────────────────────────────
    public decimal? RollingThreeMonthAmount { get; private set; } 
    public decimal? PreviousYearRollingThreeMonthAmount { get; private set; }
    public decimal? ThreeMonthYoYGrowthPercent { get; private set; }
    // ─────────────────────────────────────────────
    // Rolling 6M
    // شامل ماه جاری
    // ─────────────────────────────────────────────
    public decimal? RollingSixMonthAmount { get; private set; }
    public decimal? PreviousYearRollingSixMonthAmount { get; private set; }
    public decimal? SixMonthYoYGrowthPercent { get; private set; }
    // ─────────────────────────────────────────────
    // Fiscal YTD
    // ─────────────────────────────────────────────
    public decimal? YearToDateAmount { get; private set; }
    public decimal? PreviousYearToDateAmount { get; private set; }
    public decimal? YearToDateYoYGrowthPercent { get; private set; }
    // ─────────────────────────────────────────────
    // Historical baseline averages
    //
    // مهم:
    // ماه جاری داخل این Averageها نیست.
    // ─────────────────────────────────────────────
    public decimal? ThreeMonthAverage { get; private set; }
    public decimal? SixMonthAverage { get; private set; }
    public decimal? TwelveMonthAverage { get; private set; }
    public decimal? TwentyFourMonthAverage { get; private set; }
    public decimal? SixtyMonthAverage { get; private set; }
    // ─────────────────────────────────────────────
    // Current month distance from historical average
    // ─────────────────────────────────────────────
    public decimal? CurrentVsThreeMonthAveragePercent { get; private set; }
    public decimal? CurrentVsSixMonthAveragePercent { get; private set; }
    public decimal? CurrentVsTwelveMonthAveragePercent { get; private set; }
    public decimal? CurrentVsTwentyFourMonthAveragePercent { get; private set; }
    public decimal? CurrentVsSixtyMonthAveragePercent { get; private set; }
    // ─────────────────────────────────────────────
    // Historical monthly highs
    //
    // مهم:
    // ماه جاری داخل Highها نیست.
    // بنابراین برای RecordBroken آماده است.
    // ─────────────────────────────────────────────
    public decimal? ThreeMonthHigh { get; private set; }
    public string? ThreeMonthHighPeriod { get; private set; }
    public decimal? SixMonthHigh { get; private set; }
    public string? SixMonthHighPeriod { get; private set; }
    public decimal? TwelveMonthHigh { get; private set; }
    public string? TwelveMonthHighPeriod { get; private set; }
    public decimal? TwentyFourMonthHigh { get; private set; }
    public string? TwentyFourMonthHighPeriod { get; private set; }
    public decimal? SixtyMonthHigh { get; private set; }
    public string? SixtyMonthHighPeriod { get; private set; }
    // ─────────────────────────────────────────────
    // Distance from historical highs
    // ─────────────────────────────────────────────
    public decimal? CurrentVsThreeMonthHighPercent { get; private set; }
    public decimal? CurrentVsSixMonthHighPercent { get; private set; }
    public decimal? CurrentVsTwelveMonthHighPercent { get; private set; }
    public decimal? CurrentVsTwentyFourMonthHighPercent { get; private set; }
    public decimal? CurrentVsSixtyMonthHighPercent { get; private set; }
    // ─────────────────────────────────────────────
    // History quality
    // ─────────────────────────────────────────────
    public int AvailableHistoryMonths { get; private set; }
    public DateTimeOffset CalculatedAt { get; private set; }
    public void Update(
        string symbol,
        SalesPerformanceMetrics metrics)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentNullException.ThrowIfNull(metrics);
        Symbol = symbol;
        Apply(metrics);
    }
    private void Apply(SalesPerformanceMetrics metrics)
    {
        CurrentAmount = metrics.CurrentAmount;
        PreviousYearCurrentAmount = metrics.PreviousYearCurrentAmount;
        MonthlyYoYGrowthPercent =  metrics.MonthlyYoYGrowthPercent;
        RollingThreeMonthAmount = metrics.RollingThreeMonthAmount;
        PreviousYearRollingThreeMonthAmount = metrics.PreviousYearRollingThreeMonthAmount;
        ThreeMonthYoYGrowthPercent = metrics.ThreeMonthYoYGrowthPercent;
        RollingSixMonthAmount = metrics.RollingSixMonthAmount;
        PreviousYearRollingSixMonthAmount = metrics.PreviousYearRollingSixMonthAmount;
        SixMonthYoYGrowthPercent = metrics.SixMonthYoYGrowthPercent;
        YearToDateAmount = metrics.YearToDateAmount;
        PreviousYearToDateAmount = metrics.PreviousYearToDateAmount;
        YearToDateYoYGrowthPercent = metrics.YearToDateYoYGrowthPercent;
        ThreeMonthAverage = metrics.ThreeMonthAverage;
        SixMonthAverage = metrics.SixMonthAverage;
        TwelveMonthAverage = metrics.TwelveMonthAverage;
        TwentyFourMonthAverage = metrics.TwentyFourMonthAverage;
        SixtyMonthAverage = metrics.SixtyMonthAverage;
        CurrentVsThreeMonthAveragePercent = metrics.CurrentVsThreeMonthAveragePercent;
        CurrentVsSixMonthAveragePercent = metrics.CurrentVsSixMonthAveragePercent;
        CurrentVsTwelveMonthAveragePercent = metrics.CurrentVsTwelveMonthAveragePercent;
        CurrentVsTwentyFourMonthAveragePercent = metrics.CurrentVsTwentyFourMonthAveragePercent;
        CurrentVsSixtyMonthAveragePercent = metrics.CurrentVsSixtyMonthAveragePercent;
        ThreeMonthHigh = metrics.ThreeMonthHigh;
        ThreeMonthHighPeriod = metrics.ThreeMonthHighPeriod;
        SixMonthHigh = metrics.SixMonthHigh;
        SixMonthHighPeriod = metrics.SixMonthHighPeriod;
        TwelveMonthHigh = metrics.TwelveMonthHigh;
        TwelveMonthHighPeriod = metrics.TwelveMonthHighPeriod;
        TwentyFourMonthHigh = metrics.TwentyFourMonthHigh;
        TwentyFourMonthHighPeriod = metrics.TwentyFourMonthHighPeriod;
        SixtyMonthHigh = metrics.SixtyMonthHigh;
        SixtyMonthHighPeriod = metrics.SixtyMonthHighPeriod;
        CurrentVsThreeMonthHighPercent = metrics.CurrentVsThreeMonthHighPercent;
        CurrentVsSixMonthHighPercent = metrics.CurrentVsSixMonthHighPercent;
        CurrentVsTwelveMonthHighPercent = metrics.CurrentVsTwelveMonthHighPercent;
        CurrentVsTwentyFourMonthHighPercent = metrics.CurrentVsTwentyFourMonthHighPercent;
        CurrentVsSixtyMonthHighPercent = metrics.CurrentVsSixtyMonthHighPercent;
        AvailableHistoryMonths = metrics.AvailableHistoryMonths;
        CalculatedAt = DateTimeOffset.UtcNow;
    }
}
public sealed record SalesPerformanceMetrics
{
    public decimal? CurrentAmount { get; init; }
    public decimal? PreviousYearCurrentAmount { get; init; }
    public decimal? MonthlyYoYGrowthPercent { get; init; }
    public decimal? RollingThreeMonthAmount { get; init; }
    public decimal? PreviousYearRollingThreeMonthAmount { get; init; }
    public decimal? ThreeMonthYoYGrowthPercent { get; init; }
    public decimal? RollingSixMonthAmount { get; init; }
    public decimal? PreviousYearRollingSixMonthAmount { get; init; }
    public decimal? SixMonthYoYGrowthPercent { get; init; }
    public decimal? YearToDateAmount { get; init; }
    public decimal? PreviousYearToDateAmount { get; init; }
    public decimal? YearToDateYoYGrowthPercent { get; init; }
    public decimal? ThreeMonthAverage { get; init; }
    public decimal? SixMonthAverage { get; init; }
    public decimal? TwelveMonthAverage { get; init; }
    public decimal? TwentyFourMonthAverage { get; init; }
    public decimal? SixtyMonthAverage { get; init; }
    public decimal? CurrentVsThreeMonthAveragePercent { get; init; }
    public decimal? CurrentVsSixMonthAveragePercent { get; init; }
    public decimal? CurrentVsTwelveMonthAveragePercent { get; init; }
    public decimal? CurrentVsTwentyFourMonthAveragePercent { get; init; }
    public decimal? CurrentVsSixtyMonthAveragePercent { get; init; }
    public decimal? ThreeMonthHigh { get; init; }
    public string? ThreeMonthHighPeriod { get; init; }
    public decimal? SixMonthHigh { get; init; }
    public string? SixMonthHighPeriod { get; init; }
    public decimal? TwelveMonthHigh { get; init; }
    public string? TwelveMonthHighPeriod { get; init; }
    public decimal? TwentyFourMonthHigh { get; init; }
    public string? TwentyFourMonthHighPeriod { get; init; }
    public decimal? SixtyMonthHigh { get; init; }
    public string? SixtyMonthHighPeriod { get; init; }
    public decimal? CurrentVsThreeMonthHighPercent { get; init; }
    public decimal? CurrentVsSixMonthHighPercent { get; init; }
    public decimal? CurrentVsTwelveMonthHighPercent { get; init; }
    public decimal? CurrentVsTwentyFourMonthHighPercent { get; init; }
    public decimal? CurrentVsSixtyMonthHighPercent { get; init; }
    public int AvailableHistoryMonths { get; init; }
}