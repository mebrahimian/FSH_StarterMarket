using FSH.Modules.MarketIntelligence.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.MarketIntelligence.Data.Configurations;

public sealed class ExternalSourceSettingConfiguration
    : IEntityTypeConfiguration<ExternalSourceSetting>
{
    public void Configure(EntityTypeBuilder<ExternalSourceSetting> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("ExternalSourceSettings");

        builder.Property(x => x.Source)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Key)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Value)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.HasIndex(x => new { x.Source, x.Key })
            .IsUnique();
    }
}