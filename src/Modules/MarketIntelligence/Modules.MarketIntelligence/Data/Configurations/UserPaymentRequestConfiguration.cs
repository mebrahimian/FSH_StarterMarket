
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FSH.Modules.MarketIntelligence.Domain;
using FSH.Modules.MarketIntelligence.Domain.Enums;

namespace FSH.Modules.MarketIntelligence.Data.Configurations;

public sealed class UserPaymentRequestConfiguration
    : IEntityTypeConfiguration<UserPaymentRequest>
{
    public void Configure(EntityTypeBuilder<UserPaymentRequest> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("UserPaymentRequests", "marketintelligence");

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
        builder.Property(x => x.DurationMonths)
            .IsRequired();
        builder.Property(x => x.RequestType)
            .HasConversion<int>()
            .HasDefaultValue(UserPaymentRequestType.Purchase)
            .IsRequired();
        builder.Property(x => x.DiscountPercent)
            .HasPrecision(5, 2)
            .IsRequired();
        builder.Property(x => x.Amount)
            .HasPrecision(18, 2)
            .IsRequired();
        builder.Property(x => x.OriginalPrice)
            .HasPrecision(18, 2);

        builder.Property(x => x.RemainingCredit)
            .HasPrecision(18, 2);

        builder.Property(x => x.UpgradeFee)
            .HasPrecision(18, 2);

        builder.Property(x => x.AppliedCredit)
            .HasPrecision(18, 2);

        builder.Property(x => x.Currency)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.ReceiptFileId);

        builder.Property(x => x.BankReferenceNumber)
            .HasMaxLength(100);

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.ReviewedByUserId)
            .HasMaxLength(450);

        builder.Property(x => x.ReviewedAtUtc);

        builder.Property(x => x.ReviewNote)
            .HasMaxLength(2000);

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.UserId,
            x.CreatedAtUtc
        });

        // جلوگیری از استفاده مجدد از شناسه فایل رسید
        builder.HasIndex(x => x.ReceiptFileId)
            .IsUnique()
            .HasFilter("[ReceiptFileId] IS NOT NULL");
    }
}
