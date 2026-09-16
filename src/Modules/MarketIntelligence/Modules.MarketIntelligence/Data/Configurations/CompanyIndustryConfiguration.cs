using FSH.Modules.MarketIntelligence.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.MarketIntelligence.Data.Configurations;

public sealed class CompanyIndustryConfiguration
    : IEntityTypeConfiguration<CompanyIndustry>
{
    public void Configure(EntityTypeBuilder<CompanyIndustry> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("CompanyIndustries");

        builder.HasKey(x => x.CompanyId);

        builder.Property(x => x.CompanyId)
            .ValueGeneratedNever();

        builder.Property(x => x.IndustryId)
            .HasColumnType("char(2)")
            .HasMaxLength(2)
            .IsRequired();

        builder.Property(x => x.IndustryGroupId)
            .HasColumnType("char(3)")
            .HasMaxLength(3);

        builder.Property(x => x.Isic)
            .HasColumnType("varchar(10)")
            .HasMaxLength(10)
            .IsUnicode(false);

        builder.HasOne<Industry>()
            .WithMany()
            .HasForeignKey(x => x.IndustryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}