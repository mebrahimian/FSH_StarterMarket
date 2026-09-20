using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Modules.MarketIntelligence.Domain;

namespace FSH.Modules.MarketIntelligence.Data.Configurations;

public sealed class InstrumentValuationHistoryConfiguration :
    IEntityTypeConfiguration<InstrumentValuationHistory>
{
    public void Configure(
        EntityTypeBuilder<InstrumentValuationHistory> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(
            "InstrumentValuationHistory",
            "marketintelligence");

        builder.Property(x => x.ObservedDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.Eps)
            .HasPrecision(20, 6);

        builder.Property(x => x.PE)
            .HasPrecision(20, 6);

        builder.Property(x => x.SectorPE)
            .HasPrecision(20, 6);

        builder.Property(x => x.SalesPerShare)
            .HasPrecision(20, 6);

        builder.HasIndex(
                x => new
                {
                    x.InstrumentId,
                    x.ObservedDate
                })
            .IsUnique();

        builder.HasOne<TsetmcInstrument>()
            .WithMany()
            .HasForeignKey(x => x.InstrumentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}