using FSH.Framework.Core.Domain;

namespace FSH.Modules.MarketIntelligence.Domain;

public sealed class UserSelectedPortfolioValuationSymbol
    : BaseEntity<Guid>
{
    private UserSelectedPortfolioValuationSymbol()
    {
    }

    public string TenantId { get; private set; } = default!;
    public string UserId { get; private set; } = default!;
    public int CompanyId { get; private set; }
    public DateTime SelectedAtUtc { get; private set; }

    public static UserSelectedPortfolioValuationSymbol Create(
        string tenantId,
        string userId,
        int companyId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(companyId);

        return new UserSelectedPortfolioValuationSymbol
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            UserId = userId,
            CompanyId = companyId,
            SelectedAtUtc = DateTime.UtcNow
        };
    }
}