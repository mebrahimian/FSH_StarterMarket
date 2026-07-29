using FSH.Modules.MarketIntelligence.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.MarketIntelligence.Data.Configurations;

public sealed class DisclosureConfiguration : IEntityTypeConfiguration<Disclosure>
{
    public void Configure(EntityTypeBuilder<Disclosure> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Disclosures");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TracingNo)
            .IsRequired();

        builder.Property(x => x.Symbol)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.CompanyName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(x => x.LetterCode)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.SentDateTime)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(x => x.PublishDateTime)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(x => x.Url)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(x => x.AttachmentUrl)
            .HasMaxLength(1024);

        builder.Property(x => x.PdfUrl)
            .HasMaxLength(1024);

        builder.Property(x => x.ExcelUrl)
            .HasMaxLength(1024);

        builder.Property(x => x.XbrlUrl)
            .HasMaxLength(1024);

        builder.Property(x => x.TedanUrl)
            .HasMaxLength(1024);
       
        builder.Property(x => x.SalesParseStatus)
            .HasConversion<byte>()
            .HasColumnType("tinyint");

        builder.HasIndex(x => x.TracingNo)
            .IsUnique();

        builder.HasIndex(x => x.PublishDateTime);
    }
}