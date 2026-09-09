using FSH.Modules.MarketIntelligence.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.MarketIntelligence.Data.Configurations;

public sealed class InvestmentPortfolioReportMetadataConfiguration :
    IEntityTypeConfiguration<InvestmentPortfolioReportMetadata>
{
    public void Configure(EntityTypeBuilder<InvestmentPortfolioReportMetadata> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(
            "InvestmentPortfolioReportMetadata");

        builder.Property(x => x.PeriodEndToDate)
            .HasMaxLength(10);

        builder.Property(x => x.YearEndToDate)
            .HasMaxLength(10);

        builder.Property(x => x.Period)
            .HasMaxLength(50);

        builder.Property(x => x.Type)
            .HasMaxLength(50);

        builder.Property(x => x.TitleFa)
            .HasMaxLength(512);

        builder.Property(x => x.TitleEn)
            .HasMaxLength(512);

        builder.Property(x => x.ReportSymbol)
            .HasMaxLength(50);

        builder.Property(x => x.ReportCompanyName)
            .HasMaxLength(256);

        builder.Property(x => x.RegisteredCapital)
            .HasPrecision(28, 3);

        builder.Property(x => x.UnauthorizedCapital)
            .HasPrecision(28, 3);

        builder.Property(x => x.SourceType)
            .HasConversion<byte>()
            .HasColumnType("tinyint")
            .IsRequired();

        builder.Property(x => x.AuditStatus)
            .HasConversion<byte>()
            .HasColumnType("tinyint")
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.TracingNo,
            x.SheetCode,
            x.MetaTableId,
            x.MetaTableCode
        })
        .IsUnique();

        builder.HasIndex(x => x.DisclosureId);

        builder.HasIndex(x => x.TracingNo);
       

       
    }
}