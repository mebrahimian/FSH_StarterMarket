using FSH.Modules.MarketIntelligence.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.MarketIntelligence.Data.Configurations;

public sealed class PortfolioHoldingAssetConfiguration :
    IEntityTypeConfiguration<PortfolioHoldingAsset>
{
    public void Configure(EntityTypeBuilder<PortfolioHoldingAsset> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PortfolioHoldingAssets",
            tableBuilder => tableBuilder.HasCheckConstraint(
                               "CK_PortfolioHoldingAssets_CompanyId",
                               "[UnlistedCompanyId] IS NOT NULL OR [ListedCompanyId] IS NOT NULL"));

        builder.Property(x => x.Name)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.FSortName)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.Symbol)
            .HasMaxLength(64);

        builder.HasIndex(x => x.FSortName);

        builder.HasIndex(x => x.Symbol);

        builder.HasIndex(x => x.UnlistedCompanyId)
            .IsUnique()
            .HasFilter("[UnlistedCompanyId] IS NOT NULL");

        builder.HasIndex(x => x.ListedCompanyId)
            .IsUnique()
            .HasFilter("[ListedCompanyId] IS NOT NULL");
    }
}