using FSH.Modules.MarketIntelligence.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.MarketIntelligence.Data.Configurations;

public sealed class InvestmentPortfolioHoldingPeriodConfiguration :
    IEntityTypeConfiguration<InvestmentPortfolioHoldingPeriod>
{
    public void Configure(
        EntityTypeBuilder<InvestmentPortfolioHoldingPeriod> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("InvestmentPortfolioHoldingPeriods");

        builder.Property(x => x.ParentSymbol)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.ChangeDate)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.CurrentQuantity)
            .HasPrecision(28, 6)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.ParentSymbol,
            x.HoldingAssetId,
            x.ChangeDate
        })
        .IsUnique();

        builder.HasIndex(x => x.HoldingAssetId);

        builder.HasOne<PortfolioHoldingAsset>()
            .WithMany()
            .HasForeignKey(x => x.HoldingAssetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}