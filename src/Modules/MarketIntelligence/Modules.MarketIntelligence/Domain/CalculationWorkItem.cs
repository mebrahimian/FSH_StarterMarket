using FSH.Framework.Core.Domain;

namespace FSH.Modules.MarketIntelligence.Domain;

public sealed class CalculationWorkItem
    : BaseEntity<int>, IGlobalEntity
{
    private CalculationWorkItem()
    {
    }

    public CalculationWorkItem(
        int companyId,
        string taskType)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(companyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(taskType);

        CompanyId = companyId;
        TaskType = taskType;
        ChangeVersion = 1;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public int CompanyId { get; private set; }

    public string TaskType { get; private set; } = string.Empty;

    public long ChangeVersion { get; private set; }

    public long CompletedVersion { get; private set; }

    public long? ClaimedVersion { get; private set; }

    public Guid? LeaseToken { get; private set; }

    public DateTimeOffset? LeaseUntilUtc { get; private set; }

    public DateTimeOffset? RetryAfterUtc { get; private set; }

    public int FailureCount { get; private set; }

    public string? LastError { get; private set; }

    public DateTimeOffset? LastSuccessAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public void MarkChanged()
    {
        ChangeVersion++;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public bool TryClaim(DateTimeOffset nowUtc, TimeSpan leaseDuration)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(
              leaseDuration,
              TimeSpan.Zero);

        if (ChangeVersion <= CompletedVersion ||
            RetryAfterUtc > nowUtc ||
            LeaseUntilUtc > nowUtc)
        {
            return false;
        }

        ClaimedVersion = ChangeVersion;
        LeaseToken = Guid.NewGuid();
        LeaseUntilUtc = nowUtc.Add(leaseDuration);
        UpdatedAtUtc = nowUtc;

        return true;
    }

    public void MarkSucceeded(Guid leaseToken)
    {
        if (LeaseToken != leaseToken || ClaimedVersion is null)
        {
            throw new InvalidOperationException(
                "Invalid calculation lease.");
        }

        CompletedVersion = Math.Max(
            CompletedVersion,
            ClaimedVersion.Value);

        ClaimedVersion = null;
        LeaseToken = null;
        LeaseUntilUtc = null;
        RetryAfterUtc = null;
        FailureCount = 0;
        LastError = null;
        LastSuccessAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(
        Guid leaseToken,
        string error,
        DateTimeOffset retryAfterUtc)
    {
        if (LeaseToken != leaseToken || ClaimedVersion is null)
        {
            throw new InvalidOperationException(
                "Invalid calculation lease.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(error);

        FailureCount++;
        LastError = error.Length > 4000
            ? error[..4000]
            : error;

        RetryAfterUtc = retryAfterUtc;
        ClaimedVersion = null;
        LeaseToken = null;
        LeaseUntilUtc = null;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}