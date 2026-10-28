using FSH.Modules.MarketIntelligence.Data;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MarketIntelligence.Services.Calculations;

public sealed class CalculationWorkLeaseService(
    MarketIntelligenceDbContext dbContext)
{
    public async Task<bool> RenewAsync(
        CalculationWorkClaim claim,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(claim);

        int affected = await dbContext.Database
            .ExecuteSqlInterpolatedAsync(
                $"""
                UPDATE marketintelligence.CalculationWorkItems
                SET LeaseUntilUtc =
                        DATEADD(MINUTE, 30, SYSUTCDATETIME())
                WHERE Id = {claim.Id}
                  AND LeaseToken = {claim.LeaseToken}
                  AND ClaimedVersion = {claim.ClaimedVersion}
                  AND LeaseUntilUtc > SYSUTCDATETIME();
                """,
                cancellationToken);

        return affected == 1;
    }
}