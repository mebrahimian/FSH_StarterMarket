using FSH.Modules.MarketIntelligence.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.MarketIntelligence.Data.Configurations;

public sealed class IndustryConfiguration
    : IEntityTypeConfiguration<Industry>
{
    public void Configure(EntityTypeBuilder<Industry> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Industries", "dbo");

        builder.HasKey(x => x.IndustryId);

        builder.Property(x => x.IndustryId)
            .HasColumnType("char(2)")
            .HasMaxLength(2)
            .IsRequired();

        builder.Property(x => x.IndustryName)
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            .IsRequired();
    }
}