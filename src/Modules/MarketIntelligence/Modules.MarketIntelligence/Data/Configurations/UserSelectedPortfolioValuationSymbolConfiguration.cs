using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FSH.Modules.MarketIntelligence.Domain;

namespace FSH.Modules.MarketIntelligence.Data.Configurations;

public sealed class UserSelectedPortfolioValuationSymbolConfiguration
    : IEntityTypeConfiguration<UserSelectedPortfolioValuationSymbol>
{
    public void Configure(
        EntityTypeBuilder<UserSelectedPortfolioValuationSymbol> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(
            "UserSelectedPortfolioValuationSymbols",
            "marketintelligence");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(x => x.CompanyId)
            .IsRequired();

        builder.Property(x => x.SelectedAtUtc)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.UserId,
            x.CompanyId
        }).IsUnique();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.UserId,
            x.SelectedAtUtc
        });
    }
}