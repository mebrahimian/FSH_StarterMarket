using FSH.Modules.MarketIntelligence.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.MarketIntelligence.Data.Configurations;

public sealed class CodalProcessingLogConfiguration
    : IEntityTypeConfiguration<CodalProcessingLog>
{
    public void Configure(
        EntityTypeBuilder<CodalProcessingLog> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("CodalProcessingLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.JobId)
            .HasMaxLength(100);

        builder.Property(x => x.Operation)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Stage)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Level)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasMaxLength(30);

        builder.Property(x => x.FromDate)
            .HasMaxLength(10);

        builder.Property(x => x.ToDate)
            .HasMaxLength(10);

        builder.Property(x => x.ErrorType)
            .HasMaxLength(300);

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(x => x.Details)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.OccurredAt)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.Operation,
            x.OccurredAt
        });

        builder.HasIndex(x => new
        {
            x.Status,
            x.Level
        });

        builder.HasIndex(x => x.JobId);

        builder.HasIndex(x => x.TracingNo);
    }
}