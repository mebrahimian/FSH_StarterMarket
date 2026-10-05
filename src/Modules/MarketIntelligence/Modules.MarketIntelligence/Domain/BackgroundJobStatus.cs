using FSH.Framework.Core.Domain;

namespace FSH.Modules.MarketIntelligence.Domain;

public sealed class BackgroundJobStatus : BaseEntity<int>, IGlobalEntity
{
    private BackgroundJobStatus()
    {
    }

    public BackgroundJobStatus(
        string jobCode,
        string jobName)
    {
        JobCode = jobCode;
        JobName = jobName;
        LastStatus = "Running";
        UpdatedAt = DateTimeOffset.UtcNow;
    }
        
    public string JobCode { get; private set; } = string.Empty;

    public string JobName { get; private set; } = string.Empty;

    public DateTimeOffset? LastStartAt { get; private set; }

    public DateTimeOffset? LastEndAt { get; private set; }

    public string LastStatus { get; private set; } = string.Empty;

    public DateTimeOffset? LastSuccessAt { get; private set; }

    public DateTimeOffset? LastFailedAt { get; private set; }

    public long? LastDurationMs { get; private set; }

    public int? LastProcessed { get; private set; }

    public int? LastInserted { get; private set; }

    public int? LastUpdated { get; private set; }

    public string? LastError { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public void MarkStarted()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        LastStartAt = now;
        LastEndAt = null;
        LastStatus = "Running";
        LastError = null;
        UpdatedAt = now;
    }

    public void MarkSucceeded(
        int? processed = null,
        int? inserted = null,
        int? updated = null)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        LastEndAt = now;
        LastSuccessAt = now;
        LastStatus = "Succeeded";

        LastDurationMs =
            LastStartAt.HasValue
                ? (long)(now - LastStartAt.Value).TotalMilliseconds
                : null;

        LastProcessed = processed;
        LastInserted = inserted;
        LastUpdated = updated;
        LastError = null;
        UpdatedAt = now;
    }

    public void MarkFailed(
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        DateTimeOffset now = DateTimeOffset.UtcNow;

        LastEndAt = now;
        LastFailedAt = now;
        LastStatus = "Failed";

        LastDurationMs =
            LastStartAt.HasValue
                ? (long)(now - LastStartAt.Value).TotalMilliseconds
                : null;

        LastError = exception.ToString();
        UpdatedAt = now;
    }
}