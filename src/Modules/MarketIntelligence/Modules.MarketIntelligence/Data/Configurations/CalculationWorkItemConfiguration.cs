using FSH.Modules.MarketIntelligence.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.MarketIntelligence.Data.Configurations;

public sealed class CalculationWorkItemConfiguration
    : IEntityTypeConfiguration<CalculationWorkItem>
{
    public void Configure(
        EntityTypeBuilder<CalculationWorkItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable(
            "CalculationWorkItems",
            "marketintelligence");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TaskType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.LastError)
            .HasMaxLength(4000);

        builder.HasIndex(x => new
        {
            x.CompanyId,
            x.TaskType
        }).IsUnique();

        builder.HasIndex(x => x.RetryAfterUtc);

        builder.Property(x => x.ChangeVersion)
            .IsRequired();

        builder.Property(x => x.CompletedVersion)
            .IsRequired();

        builder.Property(x => x.FailureCount)
            .IsRequired();
    }
}