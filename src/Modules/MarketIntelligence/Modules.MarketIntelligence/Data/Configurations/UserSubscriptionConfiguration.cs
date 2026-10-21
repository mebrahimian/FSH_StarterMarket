
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FSH.Modules.MarketIntelligence.Domain;

namespace FSH.Modules.MarketIntelligence.Data.Configurations;

public sealed class UserSubscriptionConfiguration
    : IEntityTypeConfiguration<UserSubscription>
{
    public void Configure(EntityTypeBuilder<UserSubscription> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("UserSubscriptions", "marketintelligence");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(x => x.PlanKey)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.PaymentRequestId)
            .IsRequired(false);

        builder.HasIndex(x => x.PaymentRequestId)
            .IsUnique()
            .HasFilter("[PaymentRequestId] IS NOT NULL");

        builder.Property(x => x.StartUtc)
            .IsRequired();

        builder.Property(x => x.EndUtc)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.IsCancelled)
            .HasField("_isCancelled")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.UserId,
            x.StartUtc
        });
    }
}
