using FSH.Modules.MarketIntelligence.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Modules.MarketIntelligence.Domain;

namespace FSH.Modules.MarketIntelligence.Data.Configurations;

public sealed class TsetmcInstrumentConfiguration
    : IEntityTypeConfiguration<TsetmcInstrument>
{
    public void Configure(EntityTypeBuilder<TsetmcInstrument> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("TsetmcInstruments", "dbo");

        builder.Property(x => x.InsCode)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Isin)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Symbol)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.YVal)
            .HasMaxLength(16)
            .IsRequired();

        builder.HasIndex(x => x.InsCode)
            .IsUnique();

        builder.HasIndex(x => x.Isin);

        builder.HasIndex(x => x.Symbol);
    }
}