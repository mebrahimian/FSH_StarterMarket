using FSH.Framework.Core.Domain;

namespace FSH.Modules.MarketIntelligence.Domain;

public sealed class UserSubscriptionTerm : BaseEntity<Guid>, IGlobalEntity
{
    private UserSubscriptionTerm()
    {
    }

    public int DurationMonths { get; private set; }
    public decimal DiscountPercent { get; private set; }

    public static UserSubscriptionTerm Create(
        int durationMonths,
        decimal discountPercent)
    {
        if (durationMonths is not (1 or 3 or 6 or 12))
            throw new ArgumentOutOfRangeException(nameof(durationMonths));

        if (discountPercent is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(discountPercent));

        return new UserSubscriptionTerm
        {
            Id = Guid.CreateVersion7(),
            DurationMonths = durationMonths,
            DiscountPercent = discountPercent
        };
    }

    public void UpdateDiscount(decimal discountPercent)
    {
        if (discountPercent is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(discountPercent));

        DiscountPercent = discountPercent;
    }
}