
using FSH.Modules.MarketIntelligence.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Modules.MarketIntelligence.Domain;

namespace FSH.Modules.MarketIntelligence.Data.Configurations;

public sealed class CapitalIncreaseEventConfiguration
    : IEntityTypeConfiguration<CapitalIncreaseEvent>
{
    public void Configure(EntityTypeBuilder<CapitalIncreaseEvent> builder)
    {
        builder.ToTable(
            "CapitalIncreaseEvents",
            "marketintelligence",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_CapitalIncreaseEvents_Shares",
                    "[OldShares] > 0 AND [NewShares] > [OldShares]");
            });
        ArgumentNullException.ThrowIfNull(builder);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.InstrumentId)
            .IsRequired();

        builder.Property(x => x.OldShares)
            .IsRequired();

        builder.Property(x => x.NewShares)
            .IsRequired();

        builder.Property(x => x.AdjustmentEffectiveDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.AssemblyDate)
            .HasColumnType("date");

        builder.Property(x => x.RegistrationDate)
            .HasColumnType("date");

        builder.Property(x => x.PreviousCapital)
            .HasPrecision(28, 6);

        builder.Property(x => x.NewCapital)
            .HasPrecision(28, 6);

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

        builder.HasOne<TsetmcInstrument>()
            .WithMany()
            .HasForeignKey(x => x.InstrumentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new
        {
            x.InstrumentId,
            x.OldShares,
            x.NewShares
        })
            .IsUnique()
            .HasDatabaseName(
                "UX_CapitalIncreaseEvents_Instrument_Shares");

        builder.HasIndex(x => x.CodalDisclosureId)
            .IsUnique()
            .HasFilter("[CodalDisclosureId] IS NOT NULL")
            .HasDatabaseName(
                "UX_CapitalIncreaseEvents_CodalDisclosureId");
    }
}
