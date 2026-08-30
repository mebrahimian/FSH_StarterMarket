using FSH.Modules.MarketIntelligence.Contracts.v1.PortfolioMatching;
using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MarketIntelligence.Features.v1.PortfolioMatching;

public sealed class MatchPortfolioCompanyCommandHandler(
    MarketIntelligenceDbContext dbContext)
    : ICommandHandler<MatchPortfolioCompanyCommand, int>
{
    public async ValueTask<int> Handle(
        MatchPortfolioCompanyCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.RawCompanyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.FSortName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(command.CompanyId);

        string rawCompanyName = command.RawCompanyName.Trim();
        string fSortName = command.FSortName.Trim();

        var strategy =
            dbContext.Database.CreateExecutionStrategy();

        int updatedCount =
            await strategy.ExecuteAsync(
                async () =>
                {
                    await using var transaction =
                        await dbContext.Database
                            .BeginTransactionAsync(cancellationToken)
                            .ConfigureAwait(false);

                    var targetCompany =
                        await dbContext.CompanyMaster
                            .AsNoTracking()
                            .FirstOrDefaultAsync(
                                company =>
                                    company.CompanyId == command.CompanyId &&
                                    company.IsListed == command.IsListed,
                                cancellationToken)
                            .ConfigureAwait(false);

                    if (targetCompany is null)
                    {
                        throw new InvalidOperationException(
                            "The selected target company was not found.");
                    }

                    var existingAlias =
                        await dbContext.PortfolioCompanyAliases
                            .AsNoTracking()
                            .FirstOrDefaultAsync(
                                alias =>
                                    alias.FSortName == fSortName &&
                                    alias.IsListed == command.IsListed,
                                cancellationToken)
                            .ConfigureAwait(false);

                    if (existingAlias is not null &&
                        existingAlias.CompanyId != command.CompanyId)
                    {
                        throw new InvalidOperationException(
                            "This portfolio company name is already matched to another company.");
                    }

                    if (existingAlias is null)
                    {
                        dbContext.PortfolioCompanyAliases.Add(
                            new PortfolioCompanyAlias(
                                targetCompany.CompanyId,
                                targetCompany.Symbol,
                                targetCompany.FSortSymbol,
                                rawCompanyName,
                                fSortName,
                                command.IsListed));

                        await dbContext
                            .SaveChangesAsync(cancellationToken)
                            .ConfigureAwait(false);
                    }

                    int count =
                        await dbContext.InvestmentPortfolioPositions
                            .Where(position =>
                                position.ChildCompanyId == null &&
                                position.FSortName == fSortName &&
                                position.IsListed == command.IsListed)
                            .ExecuteUpdateAsync(
                                setters =>
                                    setters.SetProperty(
                                        position => position.ChildCompanyId,
                                        command.CompanyId),
                                cancellationToken)
                            .ConfigureAwait(false);

                    await transaction
                        .CommitAsync(cancellationToken)
                        .ConfigureAwait(false);

                    return count;
                })
            .ConfigureAwait(false);

        return updatedCount;
    }
}