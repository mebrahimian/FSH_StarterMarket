
using FSH.Framework.Core.Domain;

namespace FSH.Modules.MarketIntelligence.Domain;

public sealed class UserSelectedSymbol : BaseEntity<Guid>
{
    private UserSelectedSymbol()
    {
    }

    public string TenantId { get; private set; } = default!;
    public string UserId { get; private set; } = default!;

    public int CompanyId { get; private set; }

    public DateTime SelectedAtUtc { get; private set; }

    public static UserSelectedSymbol Create(
        string tenantId,
        string userId,
        int companyId,
        DateTime selectedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(companyId);

        return new UserSelectedSymbol
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            UserId = userId,
            CompanyId = companyId,
            SelectedAtUtc = selectedAtUtc.ToUniversalTime()
        };
    }
}
