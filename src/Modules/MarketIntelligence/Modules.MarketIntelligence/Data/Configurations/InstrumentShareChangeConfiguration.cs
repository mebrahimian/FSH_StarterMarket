using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Modules.MarketIntelligence.Domain;

namespace Modules.MarketIntelligence.Data.Configurations;

public sealed class InstrumentShareChangeConfiguration :
    IEntityTypeConfiguration<InstrumentShareChange>
{
    public void Configure(
        EntityTypeBuilder<InstrumentShareChange> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(
            "InstrumentShareChanges",
            "marketintelligence");

        builder.Property(x => x.EffectiveDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.OldShares)
            .IsRequired();

        builder.Property(x => x.NewShares)
            .IsRequired();

        builder.HasIndex(
                x => new
                {
                    x.InstrumentId,
                    x.EffectiveDate
                })
            .IsUnique();

        builder.HasOne<TsetmcInstrument>()
            .WithMany()
            .HasForeignKey(x => x.InstrumentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}