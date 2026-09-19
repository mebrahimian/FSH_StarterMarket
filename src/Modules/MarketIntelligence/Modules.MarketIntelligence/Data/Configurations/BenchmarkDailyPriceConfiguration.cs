using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Modules.MarketIntelligence.Domain;

namespace FSH.Modules.MarketIntelligence.Data.Configurations;

public sealed class BenchmarkDailyPriceConfiguration
    : IEntityTypeConfiguration<BenchmarkDailyPrice>
{
    public void Configure(
        EntityTypeBuilder<BenchmarkDailyPrice> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(
            "BenchmarkDailyPrices",
            "marketintelligence");

        builder.Property(x => x.TradeDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.OpenPrice)
            .HasPrecision(20, 6)
            .IsRequired();

        builder.Property(x => x.LowPrice)
            .HasPrecision(20, 6)
            .IsRequired();

        builder.Property(x => x.HighPrice)
            .HasPrecision(20, 6)
            .IsRequired();

        builder.Property(x => x.ClosePrice)
            .HasPrecision(20, 6)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.BenchmarkAssetId,
            x.TradeDate
        })
        .IsUnique();

        builder.HasOne<BenchmarkAsset>()
            .WithMany()
            .HasForeignKey(x => x.BenchmarkAssetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}