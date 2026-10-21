
using FSH.Framework.Core.Domain;

namespace FSH.Modules.MarketIntelligence.Domain;

/// <summary>
/// اشتراک شخصی کاربر در یک Tenant.
/// مستقل از اشتراک Tenantمحور ماژول Billing.
/// </summary>
public sealed class UserSubscription : BaseEntity<Guid>
{
    private UserSubscription()
    {
    }

    public string TenantId { get; private set; } = default!;
    public string UserId { get; private set; } = default!;
    public string PlanKey { get; private set; } = default!;
    public Guid? PaymentRequestId { get; private set; }
    public DateTime StartUtc { get; private set; }
    public DateTime? EndUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static UserSubscription Create(
        string tenantId,
        string userId,
        string planKey,
        DateTime startUtc,
        DateTime endUtc,
        Guid? paymentRequestId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(planKey);

        if (endUtc <= startUtc)
        {
            throw new ArgumentException(
                "EndUtc must be after StartUtc.",
                nameof(endUtc));
        }

        return new UserSubscription
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            UserId = userId,
#pragma warning disable CA1308
            PlanKey = planKey.Trim().ToLowerInvariant(),
#pragma warning restore CA1308
            StartUtc = startUtc.ToUniversalTime(),
            EndUtc = endUtc.ToUniversalTime(),
            CreatedAtUtc = DateTime.UtcNow,
            PaymentRequestId = paymentRequestId
        };
    }

    public bool IsActiveAt(DateTime utcNow)
    {
        return utcNow >= StartUtc &&
               (EndUtc is null || utcNow < EndUtc.Value);
    }

    public string GetEffectivePlanKey(DateTime utcNow)
    {
        return IsActiveAt(utcNow) ? PlanKey : "free";
    }
    private bool _isCancelled;

    public bool IsCancelled => _isCancelled;

    public void Cancel()
    {
        _isCancelled = true;
    }
    public void EndAt(DateTime endUtc)
    {
        var utc = endUtc.ToUniversalTime();

        if (utc <= StartUtc)
            throw new InvalidOperationException(
                "Subscription end must be after start.");

        if (EndUtc.HasValue && utc > EndUtc.Value)
            throw new InvalidOperationException(
                "Cannot extend subscription using EndAt.");

        EndUtc = utc;
    }
}
