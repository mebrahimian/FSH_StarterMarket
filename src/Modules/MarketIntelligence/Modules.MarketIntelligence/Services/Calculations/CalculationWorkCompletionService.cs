using FSH.Modules.MarketIntelligence.Data;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MarketIntelligence.Services.Calculations;

public sealed class CalculationWorkCompletionService(
    MarketIntelligenceDbContext dbContext)
{
    public async Task MarkSucceededAsync(
        CalculationWorkClaim claim,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(claim);

        int affected = await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE marketintelligence.CalculationWorkItems
            SET CompletedVersion =
                    CASE
                        WHEN CompletedVersion < {claim.ClaimedVersion}
                        THEN {claim.ClaimedVersion}
                        ELSE CompletedVersion
                    END,
                ClaimedVersion = NULL,
                LeaseToken = NULL,
                LeaseUntilUtc = NULL,
                RetryAfterUtc = NULL,
                FailureCount = 0,
                LastError = NULL,
                LastSuccessAtUtc = SYSUTCDATETIME(),
                UpdatedAtUtc = SYSUTCDATETIME()
            WHERE Id = {claim.Id}
              AND LeaseToken = {claim.LeaseToken}
              AND ClaimedVersion = {claim.ClaimedVersion}
              AND LeaseUntilUtc > SYSUTCDATETIME();
            """,
            cancellationToken);

        if (affected != 1)
        {
            throw new InvalidOperationException(
                $"Calculation completion rejected. WorkItemId={claim.Id}. " +
                "The lease is expired or owned by another worker.");
        }
    }

    public async Task MarkFailedAsync(
        CalculationWorkClaim claim,
        string error,
        DateTimeOffset retryAfterUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(claim);
        ArgumentException.ThrowIfNullOrWhiteSpace(error);

        string message = error.Length > 4000
            ? error[..4000]
            : error;

        int affected = await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE marketintelligence.CalculationWorkItems
            SET ClaimedVersion = NULL,
                LeaseToken = NULL,
                LeaseUntilUtc = NULL,
                RetryAfterUtc = {retryAfterUtc},
                FailureCount = FailureCount + 1,
                LastError = {message},
                UpdatedAtUtc = SYSUTCDATETIME()
            WHERE Id = {claim.Id}
              AND LeaseToken = {claim.LeaseToken}
              AND ClaimedVersion = {claim.ClaimedVersion}
              AND LeaseUntilUtc > SYSUTCDATETIME();
            """,
            cancellationToken);

        if (affected != 1)
        {
            throw new InvalidOperationException(
                $"Calculation failure update rejected. WorkItemId={claim.Id}. " +
                "The lease is expired or owned by another worker.");
        }
    }
}