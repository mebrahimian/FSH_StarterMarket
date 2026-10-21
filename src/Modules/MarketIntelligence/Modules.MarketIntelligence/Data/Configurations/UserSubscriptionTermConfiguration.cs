using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FSH.Modules.MarketIntelligence.Domain;

namespace FSH.Modules.MarketIntelligence.Data.Configurations;

public sealed class UserSubscriptionTermConfiguration
    : IEntityTypeConfiguration<UserSubscriptionTerm>
{
    public void Configure(EntityTypeBuilder<UserSubscriptionTerm> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(
            "UserSubscriptionTerms",
            "marketintelligence");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DurationMonths)
            .IsRequired();

        builder.Property(x => x.DiscountPercent)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.HasIndex(x => x.DurationMonths)
            .IsUnique();
    }
}