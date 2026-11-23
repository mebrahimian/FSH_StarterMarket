
using FSH.Modules.MarketIntelligence.Domain.Insights;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.MarketIntelligence.Data.Configurations;

public sealed class RealMoneyFlowSnapshotConfiguration :
    IEntityTypeConfiguration<RealMoneyFlowSnapshot>
{
    public void Configure(
        EntityTypeBuilder<RealMoneyFlowSnapshot> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("RealMoneyFlowSnapshots");

        builder.Property(x => x.CompanyId)
            .IsRequired();

        builder.Property(x => x.Symbol)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.PeriodEndDate)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.LastFlowTradeDate)
            .HasColumnType("date");

        // Configure all monetary and percentage fields.
        foreach (var property in
            typeof(RealMoneyFlowSnapshot).GetProperties())
        {
            Type type = property.PropertyType;

            if (type == typeof(decimal) ||
                type == typeof(decimal?))
            {
                bool isPercentage = property.Name.EndsWith(
                    "Percent",
                    StringComparison.Ordinal);

                builder.Property(property.Name)
                    .HasPrecision(
                        isPercentage ? 18 : 28,
                        4);
            }

            // Historical high/low Persian periods.
            if (property.Name.EndsWith(
                    "HighPeriod",
                    StringComparison.Ordinal) ||
                property.Name.EndsWith(
                    "LowPeriod",
                    StringComparison.Ordinal))
            {
                builder.Property(property.Name)
                    .HasMaxLength(10);
            }
        }

        // One snapshot per company and Persian month.
        builder.HasIndex(x => new
        {
            x.CompanyId,
            x.PeriodEndDate
        })
        .IsUnique();

        // Historical period queries.
        builder.HasIndex(x => x.PeriodEndDate);

        // Company history.
        builder.HasIndex(x => new
        {
            x.CompanyId,
            x.PeriodEndDate,
            x.CalculatedAt
        });
    }
}
