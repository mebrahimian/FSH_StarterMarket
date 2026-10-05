using FSH.Modules.Auditing.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Auditing.Persistence;

/// <summary>
/// Daily Hangfire job that prunes the audit table per
/// <see cref="AuditRetentionOptions"/>. Uses <c>ExecuteDeleteAsync</c> with
/// a bounded batch size so a single run doesn't take a long-held lock on
/// the table — each event-type sweep loops until fewer than batch-size
/// rows are deleted.
/// </summary>
public sealed class AuditRetentionJob
{
    private readonly AuditDbContext _db;
    private readonly AuditRetentionOptions _opts;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AuditRetentionJob> _logger;

    public AuditRetentionJob(
        AuditDbContext db,
        AuditRetentionOptions opts,
        TimeProvider timeProvider,
        ILogger<AuditRetentionJob> logger)
    {
        _db = db;
        _opts = opts;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        if (!_opts.Enabled)
        {
            _logger.LogInformation("[Auditing] retention job skipped (Enabled=false).");
            return;
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        long total = 0;
        total += await SweepAsync(AuditEventType.Activity, now.AddDays(-_opts.ActivityRetentionDays), ct).ConfigureAwait(false);
        total += await SweepAsync(AuditEventType.EntityChange, now.AddDays(-_opts.EntityChangeRetentionDays), ct).ConfigureAwait(false);
        total += await SweepAsync(AuditEventType.Security, now.AddDays(-_opts.SecurityRetentionDays), ct).ConfigureAwait(false);
        total += await SweepAsync(AuditEventType.Exception, now.AddDays(-_opts.ExceptionRetentionDays), ct).ConfigureAwait(false);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("[Auditing] retention job purged {Total} rows.", total);
        }
    }

    private async Task<long> SweepAsync(
    AuditEventType eventType,
    DateTime cutoffUtc,
    CancellationToken ct)
    {
        long swept = 0;
        var typeId = (int)eventType;
        var batchSize = Math.Max(100, _opts.DeleteBatchSize);

        while (!ct.IsCancellationRequested)
        {
            var deleted = await _db.Database.ExecuteSqlInterpolatedAsync(
                $"""
            WITH Batch AS
            (
                SELECT TOP ({batchSize}) *
                FROM [audit].[AuditRecords]
                WHERE [EventType] = {typeId}
                  AND [OccurredAtUtc] < {cutoffUtc}
                ORDER BY [OccurredAtUtc]
            )
            DELETE FROM Batch;
            """,
                ct);

            swept += deleted;

            if (deleted < batchSize)
            {
                break;
            }
        }

        return swept;
    }
}
