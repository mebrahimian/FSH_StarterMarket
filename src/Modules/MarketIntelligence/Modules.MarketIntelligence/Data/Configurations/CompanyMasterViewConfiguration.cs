using FSH.Modules.MarketIntelligence.Data.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.MarketIntelligence.Data.Configurations;

public sealed class CompanyMasterViewConfiguration :
    IEntityTypeConfiguration<CompanyMasterView>
{
    public void Configure(
        EntityTypeBuilder<CompanyMasterView> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasNoKey();

        builder.ToView(
            "vw_CompanyMaster",
            "marketintelligence");

        builder.Property(x => x.CompanyId);

        builder.Property(x => x.Symbol)
            .HasMaxLength(64);

        builder.Property(x => x.FSortSymbol)
            .HasMaxLength(64);

        builder.Property(x => x.CompanyName);

        builder.Property(x => x.FSortName);

        builder.Property(x => x.IsListed);
    }
}