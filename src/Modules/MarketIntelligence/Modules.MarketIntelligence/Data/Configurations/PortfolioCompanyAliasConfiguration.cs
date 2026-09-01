using FSH.Modules.MarketIntelligence.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.MarketIntelligence.Data.Configurations;

public sealed class PortfolioCompanyAliasConfiguration :
    IEntityTypeConfiguration<PortfolioCompanyAlias>
{
    public void Configure(
        EntityTypeBuilder<PortfolioCompanyAlias> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();
        builder.ToTable("PortfolioCompanyAliases");

        builder.Property(x => x.CompanyId).IsRequired();

        builder.Property(x => x.Symbol).HasMaxLength(64);
        builder.Property(x => x.FSortSymbol).HasMaxLength(64);

        builder.Property(x => x.AliasName).HasMaxLength(512).IsRequired();

        builder.Property(x => x.FSortName).HasMaxLength(512).IsRequired();
        builder.HasIndex(x => new {x.FSortName, x.IsListed,}).IsUnique();

        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.IsListed).HasDefaultValue(true).IsRequired();

        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.FSortSymbol);
        builder.HasIndex(x => x.FSortName);
    }
}