
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FSH.Modules.MarketIntelligence.Domain;

namespace FSH.Modules.MarketIntelligence.Data.Configurations;

public sealed class UserPlanLimitConfiguration
    : IEntityTypeConfiguration<UserPlanLimit>
{
    public void Configure(EntityTypeBuilder<UserPlanLimit> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("UserPlanLimits", "marketintelligence");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PlanKey)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.MaxSelectedSymbols)
            .IsRequired();

        builder.Property(x => x.MaxSymbolsPerIndustry)
            .IsRequired();

        // هر پلن فقط یک مجموعه محدودیت دارد.
        builder.HasIndex(x => x.PlanKey)
            .IsUnique();
    }
}
