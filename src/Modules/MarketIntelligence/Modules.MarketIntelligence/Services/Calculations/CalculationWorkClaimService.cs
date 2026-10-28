using System.Data.Common;
using FSH.Modules.MarketIntelligence.Data;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MarketIntelligence.Services.Calculations;

public sealed record CalculationWorkClaim(
    int Id,
    int CompanyId,
    string TaskType,
    long ClaimedVersion,
    Guid LeaseToken);

public sealed class CalculationWorkClaimService(
    MarketIntelligenceDbContext dbContext)
{
    public async Task<CalculationWorkClaim?> TryClaimNextAsync(
        CancellationToken cancellationToken = default)
    {
        Guid leaseToken = Guid.NewGuid();

        await dbContext.Database.OpenConnectionAsync(
            cancellationToken);

        try
        {
            await using DbCommand command =
                dbContext.Database.GetDbConnection().CreateCommand();

            command.CommandText =
                """
                ;WITH Candidate AS
                (
                    SELECT TOP (1) *
                    FROM marketintelligence.CalculationWorkItems
                        WITH (UPDLOCK, READPAST, ROWLOCK)
                    WHERE ChangeVersion > CompletedVersion
                      AND (
                          LeaseUntilUtc IS NULL
                          OR LeaseUntilUtc <= SYSUTCDATETIME()
                      )
                      AND (
                          RetryAfterUtc IS NULL
                          OR RetryAfterUtc <= SYSUTCDATETIME()
                      )
                    ORDER BY UpdatedAtUtc, Id
                )
                UPDATE Candidate
                SET ClaimedVersion = ChangeVersion,
                    LeaseToken = @leaseToken,
                    LeaseUntilUtc = DATEADD(
                        MINUTE, 30, SYSUTCDATETIME()),
                    UpdatedAtUtc = SYSUTCDATETIME()
                OUTPUT
                    INSERTED.Id,
                    INSERTED.CompanyId,
                    INSERTED.TaskType,
                    INSERTED.ClaimedVersion,
                    INSERTED.LeaseToken;
                """;

            DbParameter parameter = command.CreateParameter();
            parameter.ParameterName = "@leaseToken";
            parameter.Value = leaseToken;
            command.Parameters.Add(parameter);

            await using DbDataReader reader =
                await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return new CalculationWorkClaim(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetString(2),
                reader.GetInt64(3),
                reader.GetGuid(4));
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }
    }
}