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
        builder.Property(x => x.BuyIndividualVolume)
            .IsRequired(false);

        builder.Property(x => x.BuyIndividualValue)
            .IsRequired(false);

        builder.Property(x => x.BuyIndividualCount)
            .IsRequired(false);

        builder.Property(x => x.SellIndividualVolume)
            .IsRequired(false);

        builder.Property(x => x.SellIndividualValue)
            .IsRequired(false);

        builder.Property(x => x.SellIndividualCount)
            .IsRequired(false);

        builder.Property(x => x.BuyInstitutionalVolume)
            .IsRequired(false);

        builder.Property(x => x.BuyInstitutionalValue)
            .IsRequired(false);

        builder.Property(x => x.BuyInstitutionalCount)
            .IsRequired(false);

        builder.Property(x => x.SellInstitutionalVolume)
            .IsRequired(false);

        builder.Property(x => x.SellInstitutionalValue)
            .IsRequired(false);

        builder.Property(x => x.SellInstitutionalCount)
            .IsRequired(false);

        builder.Property(x => x.RealMoneyFlow)
            .IsRequired(false);

        builder.Property(x => x.IndividualBuyerPower)
            .HasPrecision(18, 6)
            .IsRequired(false);

        builder.Property(x => x.InstitutionalNetFlow)
            .IsRequired(false);

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