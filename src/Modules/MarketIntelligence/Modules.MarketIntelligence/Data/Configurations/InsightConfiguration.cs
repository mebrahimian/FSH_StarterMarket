using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FSH.Modules.MarketIntelligence.Domain.Insights;

namespace FSH.Modules.MarketIntelligence.Persistence.Configurations;

public sealed class InsightConfiguration : IEntityTypeConfiguration<Insight>
{
    public void Configure(EntityTypeBuilder<Insight> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Insights");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Symbol)
            .HasMaxLength(50);

        builder.Property(x => x.PeriodEndDate)
            .HasMaxLength(10);

        builder.Property(x => x.Direction)
            .HasConversion<byte>();

        builder.Property(x => x.ConfidenceScore)
            .HasPrecision(5, 2);

        builder.Property(x => x.ImpactScore)
            .HasPrecision(5, 2);

        builder.Property(x => x.PayloadJson)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.HasIndex(x => x.Code);

        builder.HasIndex(x => new
        {
            x.CompanyId,
            x.PeriodEndDate
        });
    }
}