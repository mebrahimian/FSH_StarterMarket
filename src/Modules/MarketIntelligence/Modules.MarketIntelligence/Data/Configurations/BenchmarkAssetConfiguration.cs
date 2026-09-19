using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Modules.MarketIntelligence.Domain;

namespace FSH.Modules.MarketIntelligence.Data.Configurations;

public sealed class BenchmarkAssetConfiguration
    : IEntityTypeConfiguration<BenchmarkAsset>
{
    public void Configure(
        EntityTypeBuilder<BenchmarkAsset> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(
            "BenchmarkAssets",
            "marketintelligence");

        builder.Property(x => x.Code)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.Currency)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.Unit)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.HasIndex(x => x.Code)
            .IsUnique();
    }
}