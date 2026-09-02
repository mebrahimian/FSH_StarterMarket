using FSH.Modules.MarketIntelligence.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.MarketIntelligence.Data.Configurations;

public sealed class InvestmentPortfolioPositionConfiguration :
    IEntityTypeConfiguration<InvestmentPortfolioPosition>
{
    public void Configure(
        EntityTypeBuilder<InvestmentPortfolioPosition> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("InvestmentPortfolioPositions");
        builder.Property(x => x.FSortName)
               .HasMaxLength(512)
               .IsRequired();

        builder.Property(x => x.ParentCompanyId)
            .IsRequired();

        builder.Property(x => x.ChildCompanyId);

        builder.Property(x => x.RawCompanyName)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.PeriodEndDate)
            .HasMaxLength(10)
            .IsRequired();
        builder.Property(x => x.SourceType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.AuditStatus)
            .HasConversion<int>()
            .IsRequired();
        builder.Property(x => x.Capital).HasPrecision(28, 6);
        builder.Property(x => x.NominalValue).HasPrecision(28, 6);

        builder.Property(x => x.BeginningQuantity).HasPrecision(28, 6);
        builder.Property(x => x.BeginningCost).HasPrecision(28, 6);
        builder.Property(x => x.BeginningMarketValue).HasPrecision(28, 6);

        builder.Property(x => x.ChangeQuantity).HasPrecision(28, 6);
        builder.Property(x => x.ChangeCost).HasPrecision(28, 6);
        builder.Property(x => x.ChangeMarketValue).HasPrecision(28, 6);

        builder.Property(x => x.OwnershipPercent).HasPrecision(18, 6);

        builder.Property(x => x.EndingQuantity).HasPrecision(28, 6);
        builder.Property(x => x.EndingCost).HasPrecision(28, 6);
        builder.Property(x => x.EndingMarketValue).HasPrecision(28, 6);
        builder.Property(x => x.EndingCostPerShare).HasPrecision(28, 6);
        builder.Property(x => x.EndingMarketPrice).HasPrecision(28, 6);

        builder.Property(x => x.IncreaseDecrease).HasPrecision(28, 6);

        builder.Property(x => x.Notes)
            .HasMaxLength(4000);

        builder.HasIndex(x => new
        {
            x.ParentCompanyId,
            x.PeriodEndDate
        });

        builder.HasIndex(x => x.ChildCompanyId);

        builder.HasIndex(x => x.TracingNo);

        builder.HasIndex(x => x.DisclosureId);
        builder.HasIndex(x => x.FSortName);
    }
}