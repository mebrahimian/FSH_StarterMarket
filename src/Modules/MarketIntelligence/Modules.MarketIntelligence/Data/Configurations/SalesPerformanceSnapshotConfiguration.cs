using FSH.Modules.MarketIntelligence.Domain.Insights;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.MarketIntelligence.Data.Configurations;

public sealed class SalesPerformanceSnapshotConfiguration :
    IEntityTypeConfiguration<SalesPerformanceSnapshot>
{
    public void Configure(EntityTypeBuilder<SalesPerformanceSnapshot> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("SalesPerformanceSnapshots");

        builder.Property(x => x.CompanyId)
            .IsRequired();

        builder.Property(x => x.Symbol)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.PeriodEndDate)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.ThreeMonthHighPeriod)
            .HasMaxLength(10);

        builder.Property(x => x.SixMonthHighPeriod)
            .HasMaxLength(10);

        builder.Property(x => x.TwelveMonthHighPeriod)
            .HasMaxLength(10);

        builder.Property(x => x.TwentyFourMonthHighPeriod)
            .HasMaxLength(10);

        builder.Property(x => x.SixtyMonthHighPeriod)
            .HasMaxLength(10);

        ConfigureAmounts(builder);
        ConfigurePercentages(builder);

        builder.HasIndex(x => new
        {
            x.CompanyId,
            x.PeriodEndDate
        })
        .IsUnique();

        builder.HasIndex(x => x.PeriodEndDate);
    }

    private static void ConfigureAmounts(EntityTypeBuilder<SalesPerformanceSnapshot> builder)
    {
        builder.Property(x => x.CurrentAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.PreviousYearCurrentAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.RollingThreeMonthAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.PreviousYearRollingThreeMonthAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.RollingSixMonthAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.PreviousYearRollingSixMonthAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.YearToDateAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.PreviousYearToDateAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.ThreeMonthAverage)
            .HasPrecision(18, 2);

        builder.Property(x => x.SixMonthAverage)
            .HasPrecision(18, 2);

        builder.Property(x => x.TwelveMonthAverage)
            .HasPrecision(18, 2);

        builder.Property(x => x.TwentyFourMonthAverage)
            .HasPrecision(18, 2);

        builder.Property(x => x.SixtyMonthAverage)
            .HasPrecision(18, 2);

        builder.Property(x => x.ThreeMonthHigh)
            .HasPrecision(18, 2);

        builder.Property(x => x.SixMonthHigh)
            .HasPrecision(18, 2);

        builder.Property(x => x.TwelveMonthHigh)
            .HasPrecision(18, 2);

        builder.Property(x => x.TwentyFourMonthHigh)
            .HasPrecision(18, 2);

        builder.Property(x => x.SixtyMonthHigh)
            .HasPrecision(18, 2);
    }
    private static void ConfigurePercentages(
        EntityTypeBuilder<SalesPerformanceSnapshot> builder)
    {
        builder.Property(x => x.MonthlyYoYGrowthPercent)
            .HasPrecision(18, 2);

        builder.Property(x => x.ThreeMonthYoYGrowthPercent)
            .HasPrecision(18, 2);

        builder.Property(x => x.SixMonthYoYGrowthPercent)
            .HasPrecision(18, 2);

        builder.Property(x => x.YearToDateYoYGrowthPercent)
            .HasPrecision(18, 2);

        builder.Property(x => x.CurrentVsThreeMonthAveragePercent)
            .HasPrecision(18, 2);

        builder.Property(x => x.CurrentVsSixMonthAveragePercent)
            .HasPrecision(18, 2);

        builder.Property(x => x.CurrentVsTwelveMonthAveragePercent)
            .HasPrecision(18, 2);

        builder.Property(x => x.CurrentVsTwentyFourMonthAveragePercent)
            .HasPrecision(18, 2);

        builder.Property(x => x.CurrentVsSixtyMonthAveragePercent)
            .HasPrecision(18, 2);

        builder.Property(x => x.CurrentVsThreeMonthHighPercent)
            .HasPrecision(18, 2);

        builder.Property(x => x.CurrentVsSixMonthHighPercent)
            .HasPrecision(18, 2);

        builder.Property(x => x.CurrentVsTwelveMonthHighPercent)
            .HasPrecision(18, 2);

        builder.Property(x => x.CurrentVsTwentyFourMonthHighPercent)
            .HasPrecision(18, 2);

        builder.Property(x => x.CurrentVsSixtyMonthHighPercent)
            .HasPrecision(18, 2);
    }
}