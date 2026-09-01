using FSH.Modules.MarketIntelligence.Contracts.v1.PortfolioMatching;
using FSH.Modules.MarketIntelligence.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MarketIntelligence.Features.v1.PortfolioMatching;

public sealed class UnmatchPortfolioCompanyCommandHandler(
    MarketIntelligenceDbContext dbContext)
    : ICommandHandler<UnmatchPortfolioCompanyCommand, int>
{
    public async ValueTask<int> Handle(
        UnmatchPortfolioCompanyCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.FSortName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            command.CompanyId);

        string fSortName =
            command.FSortName.Trim();

        var strategy =
            dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(
            async () =>
            {
                await using var transaction =
                    await dbContext.Database
                        .BeginTransactionAsync(cancellationToken)
                        .ConfigureAwait(false);

                var alias =
                    await dbContext.PortfolioCompanyAliases
                        .FirstOrDefaultAsync(
                            x =>
                                x.FSortName == fSortName &&
                                x.IsListed == command.IsListed &&
                                x.CompanyId == command.CompanyId,
                            cancellationToken)
                        .ConfigureAwait(false);

                if (alias is not null)
                {
                    dbContext.PortfolioCompanyAliases.Remove(alias);

                    await dbContext.SaveChangesAsync(
                            cancellationToken)
                        .ConfigureAwait(false);
                }

                int count =
                    await dbContext.InvestmentPortfolioPositions
                        .Where(position =>
                            position.FSortName == fSortName &&
                            position.IsListed == command.IsListed &&
                            position.ChildCompanyId ==
                                command.CompanyId)
                        .ExecuteUpdateAsync(
                            setters =>
                                setters.SetProperty(
                                    position =>
                                        position.ChildCompanyId,
                                    (int?)null),
                            cancellationToken)
                        .ConfigureAwait(false);

                await transaction
                    .CommitAsync(cancellationToken)
                    .ConfigureAwait(false);

                return count;
            })
            .ConfigureAwait(false);
    }
}