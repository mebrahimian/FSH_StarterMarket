using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace FSH.Modules.MarketIntelligence.Data.Configurations;

public sealed class MonthlyActivitySummaryConfiguration :
    IEntityTypeConfiguration<MonthlyActivitySummary>
{
    public void Configure(
        EntityTypeBuilder<MonthlyActivitySummary> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("MonthlyActivitySummaries");

        builder.Property(x => x.Symbol)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.PeriodEndDate)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.YearEndDate)
            .HasMaxLength(10);

        builder.Property(x => x.PeriodAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.YearToDateAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.Rt)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.Symbol,
            x.PeriodEndDate
        })
        .IsUnique();
    }
}