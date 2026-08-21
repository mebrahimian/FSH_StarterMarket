using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Auditing.Persistence;

public sealed class AuditSqlCleanupJob(
    AuditDbContext dbContext,
    ILogger<AuditSqlCleanupJob> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            DELETE TOP (5000)
            FROM audit.AuditRecords
            WHERE EventType IN (1, 3)
              AND OccurredAtUtc < DATEADD(DAY, -1, SYSUTCDATETIME());
            """;

        var totalDeleted = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var deleted = await dbContext.Database
                .ExecuteSqlRawAsync(sql, cancellationToken)
                .ConfigureAwait(false);

            totalDeleted += deleted;

            if (deleted == 0)
            {
                break;
            }
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "[Auditing] SQL cleanup deleted {RecordCount} old audit records.",
                totalDeleted);
        }
    }
}