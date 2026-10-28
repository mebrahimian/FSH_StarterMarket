using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Modules.MarketIntelligence.Domain;

namespace Modules.MarketIntelligence.Data.Configurations;

public sealed class PortfolioValuationSnapshotConfiguration
    : IEntityTypeConfiguration<PortfolioValuationSnapshot>
{
    public void Configure(
        EntityTypeBuilder<PortfolioValuationSnapshot> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable(
            "PortfolioValuationSnapshots",
            "marketintelligence");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        // One snapshot per company and reporting period
        builder.HasIndex(x => new
        {
            x.CompanyId,
            x.PeriodEndDate
        })
        .IsUnique();

        builder.Property(x => x.PeriodEndDate)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(x => x.PeriodEndDate);

        builder.Property(x => x.PreviousPeriodEndDate)
            .HasMaxLength(20);

        builder.Property(x => x.CurrentPriceTradeDate)
            .HasMaxLength(20);

        builder.Property(x => x.PreviousPriceTradeDate)
            .HasMaxLength(20);

        // Current valuation
        builder.Property(x => x.CurrentRegisteredCapital)
            .HasPrecision(28, 6);

        builder.Property(x => x.CurrentListedValue)
            .HasPrecision(28, 6);

        builder.Property(x => x.CurrentUnlistedValue)
            .HasPrecision(28, 6);

        builder.Property(x => x.CurrentPortfolioValue)
            .HasPrecision(28, 6);

        builder.Property(x => x.CurrentValuePerShare)
            .HasPrecision(28, 6);

        builder.Property(x => x.CurrentSharePrice)
            .HasPrecision(28, 6);

        builder.Property(x => x.CurrentPriceToPortfolioValuePercent)
            .HasPrecision(28, 6);

        // Previous valuation
        builder.Property(x => x.PreviousRegisteredCapital)
            .HasPrecision(28, 6);

        builder.Property(x => x.PreviousListedValue)
            .HasPrecision(28, 6);

        builder.Property(x => x.PreviousUnlistedValue)
            .HasPrecision(28, 6);

        builder.Property(x => x.PreviousPortfolioValue)
            .HasPrecision(28, 6);

        builder.Property(x => x.PreviousValuePerShare)
            .HasPrecision(28, 6);

        builder.Property(x => x.PreviousSharePrice)
            .HasPrecision(28, 6);

        builder.Property(x => x.PreviousPriceToPortfolioValuePercent)
            .HasPrecision(28, 6);

        builder.Ignore(x => x.DomainEvents);
    }
}