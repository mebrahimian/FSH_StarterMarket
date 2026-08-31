using FSH.Modules.MarketIntelligence.Contracts.v1.PortfolioMatching;
using FSH.Modules.MarketIntelligence.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Modules.MarketIntelligence.Features.v1.PortfolioMatching;

public sealed class GetMatchedPortfolioCompaniesQueryHandler(
    MarketIntelligenceDbContext dbContext)
    : IQueryHandler<
        GetMatchedPortfolioCompaniesQuery,
        IReadOnlyList<MatchedPortfolioCompanyDto>>
{
    public async ValueTask<IReadOnlyList<MatchedPortfolioCompanyDto>> Handle(
        GetMatchedPortfolioCompaniesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var positions =
            await dbContext.InvestmentPortfolioPositions
                .AsNoTracking()
                .Where(position =>
                    position.ChildCompanyId != null)
                .Select(position => new
                {
                    position.RawCompanyName,
                    position.FSortName,
                    position.IsListed,
                    CompanyId = position.ChildCompanyId!.Value,
                    position.ParentCompanyId,
                    position.PeriodEndDate,
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        int[] companyIds =
            positions
                .Select(position => position.CompanyId)
                .Distinct()
                .ToArray();

        var companyRows = await dbContext.CompanyMaster
        .AsNoTracking()
        .Select(company => new
        {
            company.CompanyId,
            company.Symbol,
            company.CompanyName,
        })
        .ToListAsync(cancellationToken)
        .ConfigureAwait(false);

        var companyIdSet =
            companyIds.ToHashSet();

        var companies =
            companyRows
                .Where(company =>
                    companyIdSet.Contains(company.CompanyId))
                .ToDictionary(company =>
                    company.CompanyId);

        var aliasRows = await dbContext.PortfolioCompanyAliases
        .AsNoTracking()
        .Where(alias =>
            alias.IsActive)
        .Select(alias => new
        {
            alias.FSortName,
            alias.IsListed,
            alias.CompanyId,
        })
        .ToListAsync(cancellationToken)
        .ConfigureAwait(false);

        var aliases =
            aliasRows
                .Where(alias =>
                    companyIdSet.Contains(alias.CompanyId))
                .ToList();

        var aliasKeys =
            aliases
                .Select(alias => (
                    alias.FSortName,
                    alias.IsListed,
                    alias.CompanyId))
                .ToHashSet();

        IReadOnlyList<MatchedPortfolioCompanyDto> result =
            positions
                .GroupBy(position => new
                {
                    position.FSortName,
                    position.IsListed,
                    position.CompanyId,
                })
                .Select(group =>
                {
                    companies.TryGetValue(
                        group.Key.CompanyId,
                        out var company);

                    string rawCompanyName =
                        group
                            .GroupBy(position =>
                                position.RawCompanyName)
                            .OrderByDescending(rawGroup =>
                                rawGroup.Count())
                            .ThenBy(rawGroup =>
                                rawGroup.Key)
                            .Select(rawGroup =>
                                rawGroup.Key)
                            .First();

                    bool hasAlias =
                        aliasKeys.Contains((
                            group.Key.FSortName,
                            group.Key.IsListed,
                            group.Key.CompanyId));

                    return new MatchedPortfolioCompanyDto(
                        rawCompanyName,
                        group.Key.FSortName,
                        group.Key.IsListed,
                        group.Key.CompanyId,
                        company?.Symbol,
                        company?.CompanyName,
                        group.Count(),
                        group
                            .Select(position =>
                                position.ParentCompanyId)
                            .Distinct()
                            .Count(),
                        group
                            .Select(position =>
                                position.PeriodEndDate)
                            .OrderBy(period =>
                                period,
                                StringComparer.Ordinal)
                            .First(),
                        group
                            .Select(position =>
                                position.PeriodEndDate)
                            .OrderByDescending(period =>
                                period,
                                StringComparer.Ordinal)
                            .First(),
                        hasAlias);
                })
                .OrderByDescending(item =>
                    item.ParentSymbolCount)
                .ThenByDescending(item =>
                    item.OccurrenceCount)
                .ThenBy(item =>
                    item.RawCompanyName)
                .ToList();

        return result;
    }
}