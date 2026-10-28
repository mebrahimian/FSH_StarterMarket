using System.Data.Common;
using FSH.Modules.MarketIntelligence.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FSH.Modules.MarketIntelligence.Services.Calculations;

public static class CalculationCompanyLock
{
    public static async Task AcquireAsync(
        MarketIntelligenceDbContext dbContext,
        int companyId,
        string taskType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(companyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(taskType);

        IDbContextTransaction transaction =
            dbContext.Database.CurrentTransaction
            ?? throw new InvalidOperationException(
                "An active database transaction is required.");

        await using DbCommand command =
            dbContext.Database.GetDbConnection().CreateCommand();

        command.Transaction = transaction.GetDbTransaction();

        command.CommandText =
            """
            DECLARE @result int;

            EXEC @result = sys.sp_getapplock
                @Resource = @resource,
                @LockMode = 'Exclusive',
                @LockOwner = 'Transaction',
                @LockTimeout = 30000;

            SELECT @result;
            """;

        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = "@resource";
        parameter.Value = $"MI:{taskType}:{companyId}";
        command.Parameters.Add(parameter);

        object? result =
            await command.ExecuteScalarAsync(cancellationToken);

        if (result is not int status || status < 0)
        {
            throw new InvalidOperationException(
                $"Failed to acquire calculation lock. " +
                $"CompanyId={companyId}, TaskType={taskType}, " +
                $"Result={result}");
        }
    }
}