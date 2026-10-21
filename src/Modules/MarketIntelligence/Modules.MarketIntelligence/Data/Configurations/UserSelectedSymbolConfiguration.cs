
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FSH.Modules.MarketIntelligence.Domain;

namespace FSH.Modules.MarketIntelligence.Data.Configurations;

public sealed class UserSelectedSymbolConfiguration
    : IEntityTypeConfiguration<UserSelectedSymbol>
{
    public void Configure(EntityTypeBuilder<UserSelectedSymbol> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("UserSelectedSymbols", "marketintelligence");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(x => x.CompanyId)
            .IsRequired();

        builder.Property(x => x.SelectedAtUtc)
            .IsRequired();

        // هر کاربر هر شرکت را فقط یک بار می‌تواند انتخاب کند.
        builder.HasIndex(x => new
        {
            x.TenantId,
            x.UserId,
            x.CompanyId
        }).IsUnique();

        // بازیابی انتخاب‌ها به ترتیب ثبت.
        builder.HasIndex(x => new
        {
            x.TenantId,
            x.UserId,
            x.SelectedAtUtc
        });
    }
}
