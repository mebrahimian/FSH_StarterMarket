using FSH.Modules.MarketIntelligence.Data;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MarketIntelligence.Services.Calculations;

public sealed class CalculationChangeService(
    MarketIntelligenceDbContext dbContext)
{
    public async Task MarkChangedAsync(
        int companyId,
        string taskType,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(companyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(taskType);

        async Task UpdateAsync()
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                UPDATE marketintelligence.CalculationWorkItems
                    WITH (UPDLOCK, HOLDLOCK)
                SET ChangeVersion = ChangeVersion + 1,
                    UpdatedAtUtc = SYSDATETIMEOFFSET()
                WHERE CompanyId = {companyId}
                  AND TaskType = {taskType};

                IF @@ROWCOUNT = 0
                BEGIN
                    INSERT INTO marketintelligence.CalculationWorkItems
                    (
                        CompanyId,
                        TaskType,
                        ChangeVersion,
                        CompletedVersion,
                        FailureCount,
                        UpdatedAtUtc
                    )
                    VALUES
                    (
                        {companyId},
                        {taskType},
                        1,
                        0,
                        0,
                        SYSDATETIMEOFFSET()
                    );
                END
                """,
                cancellationToken);
        }

        if (dbContext.Database.CurrentTransaction is not null)
        {
            await UpdateAsync();
            return;
        }

        await dbContext.Database
            .CreateExecutionStrategy()
            .ExecuteAsync(async () =>
            {
                await using var transaction =
                    await dbContext.Database.BeginTransactionAsync(
                        cancellationToken);

                await UpdateAsync();

                await transaction.CommitAsync(cancellationToken);
            });
    }
}