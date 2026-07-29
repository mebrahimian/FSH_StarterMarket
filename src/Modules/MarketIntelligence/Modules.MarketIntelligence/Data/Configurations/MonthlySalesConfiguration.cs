using FSH.Modules.MarketIntelligence.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.MarketIntelligence.Domain.Configurations;

public sealed class MonthlySalesConfiguration : IEntityTypeConfiguration<MonthlySales>
{
    public void Configure(EntityTypeBuilder<MonthlySales> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("MonthlySales");
        builder.Property(x => x.Symbol)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.PeriodEndDate)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.YearEndDate)
            .HasMaxLength(10);

        builder.Property(x => x.MonthlySalesAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.YearToDateSalesAmount)
            .HasPrecision(18, 2);

        builder.HasIndex(x => new
        {
            x.Symbol,
            x.PeriodEndDate
        })
        .IsUnique();
    }
}