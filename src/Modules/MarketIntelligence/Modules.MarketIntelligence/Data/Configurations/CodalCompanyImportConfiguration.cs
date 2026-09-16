using FSH.Modules.MarketIntelligence.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.MarketIntelligence.Data.Configurations;

public sealed class CodalCompanyImportConfiguration
    : IEntityTypeConfiguration<CodalCompanyImport>
{
    public void Configure(EntityTypeBuilder<CodalCompanyImport> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("CodalCompanyImport", "dbo");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Symbol)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.CompanyName)
            .HasMaxLength(300);

        builder.Property(x => x.IndustryId)
            .HasMaxLength(2)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(x => x.Isic)
            .HasMaxLength(10)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(x => x.IndustryGroupId)
            .HasMaxLength(3)
            .IsUnicode(false)
            .IsRequired();

        builder.HasIndex(x => x.Symbol);
    }
}