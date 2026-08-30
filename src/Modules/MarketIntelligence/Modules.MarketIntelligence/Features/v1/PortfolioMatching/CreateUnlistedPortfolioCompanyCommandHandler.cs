using System.Data;
using FSH.Framework.Shared.Utilities;
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
        string fSortName = FSort.Normalize(companyName);

        await dbContext.Database
            .OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            await using var dbCommand =
                dbContext.Database.GetDbConnection().CreateCommand();

            dbCommand.CommandText =
                """
                INSERT INTO bors.dbo.MasterInfo_NoBors
                (
                    Name_NoBors,
                    FSortName_NoBors
                )
                OUTPUT INSERTED.CompanyId_NoBors
                VALUES
                (
                    @Name,
                    @FSortName
                );
                """;

            var nameParameter = dbCommand.CreateParameter();
            nameParameter.ParameterName = "@Name";
            nameParameter.DbType = DbType.String;
            nameParameter.Value = companyName;
            dbCommand.Parameters.Add(nameParameter);

            var fSortParameter = dbCommand.CreateParameter();
            fSortParameter.ParameterName = "@FSortName";
            fSortParameter.DbType = DbType.String;
            fSortParameter.Value = fSortName;
            dbCommand.Parameters.Add(fSortParameter);

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