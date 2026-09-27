using System.Data;
using FSH.Modules.MarketIntelligence.Contracts.v1.PortfolioMatching;
using FSH.Modules.MarketIntelligence.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MarketIntelligence.Features.v1.PortfolioMatching;

public sealed class CreateUnlistedPortfolioCompanyCommandHandler(
    MarketIntelligenceDbContext dbContext)
    : ICommandHandler<
        CreateUnlistedPortfolioCompanyCommand,
        PortfolioCompanyTargetDto>
{
    public async ValueTask<PortfolioCompanyTargetDto> Handle(
        CreateUnlistedPortfolioCompanyCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.RawCompanyName);

        string companyName = command.RawCompanyName.Trim();
       
        await dbContext.Database
            .OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            await using var dbCommand =
                dbContext.Database.GetDbConnection().CreateCommand();

            dbCommand.CommandText =
                """
                INSERT INTO marketintelligence.MasterInfo_NoBors
                (
                    CompanyName,
                    NormalizedName
                )
                OUTPUT INSERTED.CompanyId
                VALUES
                (
                    @Name,
                    dbo.NormalizeForMatch(@Name)
                );
                """;

            var nameParameter = dbCommand.CreateParameter();
            nameParameter.ParameterName = "@Name";
            nameParameter.DbType = DbType.String;
            nameParameter.Value = companyName;

            dbCommand.Parameters.Add(nameParameter);

            object? result = await dbCommand
                    .ExecuteScalarAsync(cancellationToken)
                    .ConfigureAwait(false);

            if (result is not int companyId)
            {
                throw new InvalidOperationException(
                    "Could not retrieve the new unlisted company id.");
            }

            return new PortfolioCompanyTargetDto(
                companyId,
                null,
                companyName,
                false);
        }
        finally
        {
            await dbContext.Database
                .CloseConnectionAsync()
                .ConfigureAwait(false);
        }
    }
}