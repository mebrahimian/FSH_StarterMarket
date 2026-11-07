using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FSH.Modules.MarketIntelligence.Domain;

namespace FSH.Modules.MarketIntelligence.Data.Configurations;

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
        // Codal registration metadata

        builder.Property(x => x.CodalDisclosureId)
            .IsRequired(false);

        builder.Property(x => x.AssemblyDate)
            .HasColumnType("date");

        builder.Property(x => x.RegistrationDate)
            .HasColumnType("date");

        // Registered capital

        builder.Property(x => x.PreviousCapital)
            .HasPrecision(28, 6);

        builder.Property(x => x.NewCapital)
            .HasPrecision(28, 6);

        // Capital increase sources

        builder.Property(x => x.CashIncoming)
            .HasPrecision(28, 6);

        builder.Property(x => x.RetainedEarnings)
            .HasPrecision(28, 6);

        builder.Property(x => x.Reserves)
            .HasPrecision(28, 6);

        builder.Property(x => x.SharePremium)
            .HasPrecision(28, 6);

        builder.Property(x => x.RevaluationSurplus)
            .HasPrecision(28, 6);

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