using FSH.Framework.Core.Domain;

namespace FSH.Modules.MarketIntelligence.Domain;

public sealed class CodalProcessingLog
    : BaseEntity<long>, IGlobalEntity
{
    private CodalProcessingLog() { }

    public string? JobId { get; private set; }

    public string Operation { get; private set; } = string.Empty;
    public string Stage { get; private set; } = string.Empty;

    public byte Level { get; private set; }

    public string? Status { get; private set; }

    public string? FromDate { get; private set; }
    public string? ToDate { get; private set; }

    public int? PageNumber { get; private set; }
    public long? TracingNo { get; private set; }

    public string? ErrorType { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? Details { get; private set; }

    public int? AttemptCount { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }

    public CodalProcessingLog(
        string operation,
        string stage,
        byte level,
        string? status = null,
        string? jobId = null,
        string? fromDate = null,
        string? toDate = null,
        int? pageNumber = null,
        long? tracingNo = null,
        string? errorType = null,
        string? errorMessage = null,
        string? details = null,
        int? attemptCount = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(stage);

        Operation = operation;
        Stage = stage;
        Level = level;
        Status = status;
        JobId = jobId;

        FromDate = fromDate;
        ToDate = toDate;
        PageNumber = pageNumber;
        TracingNo = tracingNo;

        ErrorType = errorType;
        ErrorMessage = errorMessage;
        Details = details;
        AttemptCount = attemptCount;

        OccurredAt = DateTimeOffset.UtcNow;
    }

    public void MarkResolved()
    {
        Status = "Resolved";
        ResolvedAt = DateTimeOffset.UtcNow;
    }
    public void UpdateProgress(
    string fromDate,
    string toDate,
    int pageNumber)
    {
        if (Operation != "Historical" || Stage != "Progress")
        {
            throw new InvalidOperationException(
                "This record is not a Historical progress record.");
        }

        FromDate = fromDate;
        ToDate = toDate;
        PageNumber = pageNumber;
        OccurredAt = DateTimeOffset.UtcNow;
    }

    public void CompleteProgress()
    {
        if (Operation != "Historical" || Stage != "Progress")
        {
            throw new InvalidOperationException(
                "This record is not a Historical progress record.");
        }

        Status = "Completed";
        OccurredAt = DateTimeOffset.UtcNow;
    }
}