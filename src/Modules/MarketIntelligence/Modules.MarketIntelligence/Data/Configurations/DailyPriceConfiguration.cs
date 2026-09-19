using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Modules.MarketIntelligence.Domain;

namespace FSH.Modules.MarketIntelligence.Data.Configurations;

public sealed class DailyPriceConfiguration
    : IEntityTypeConfiguration<DailyPrice>
{
    public void Configure(EntityTypeBuilder<DailyPrice> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(
            "DailyPrices",
            "marketintelligence");

        builder.Property(x => x.TradeDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.FirstPrice)
            .IsRequired();

        builder.Property(x => x.LowPrice)
            .IsRequired();

        builder.Property(x => x.HighPrice)
            .IsRequired();

        builder.Property(x => x.ClosingPrice)
            .IsRequired();

        builder.Property(x => x.LastPrice)
            .IsRequired();

        builder.Property(x => x.YesterdayPrice)
            .IsRequired();

        builder.Property(x => x.TradeCount)
            .IsRequired();

        builder.Property(x => x.Volume)
            .IsRequired();

        builder.Property(x => x.Value)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.InstrumentId,
            x.TradeDate
        })
        .IsUnique();

        builder.HasOne<TsetmcInstrument>()
            .WithMany()
            .HasForeignKey(x => x.InstrumentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}