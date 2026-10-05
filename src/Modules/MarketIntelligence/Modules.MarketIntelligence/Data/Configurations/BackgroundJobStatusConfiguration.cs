using FSH.Modules.MarketIntelligence.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.MarketIntelligence.Persistence.Configurations;

public sealed class BackgroundJobStatusConfiguration :
    IEntityTypeConfiguration<BackgroundJobStatus>
{
    public void Configure(EntityTypeBuilder<BackgroundJobStatus> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable(
            "BackgroundJobStatuses",
            "marketintelligence");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.JobCode)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.JobName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.LastStatus)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.LastError)
            .HasMaxLength(4000);

        builder.HasIndex(x => x.JobCode)
            .IsUnique();
    }
}