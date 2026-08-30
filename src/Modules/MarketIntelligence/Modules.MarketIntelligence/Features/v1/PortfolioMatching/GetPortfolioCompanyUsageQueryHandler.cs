using FSH.Modules.MarketIntelligence.Contracts.v1.PortfolioMatching;
using FSH.Modules.MarketIntelligence.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Modules.MarketIntelligence.Features.v1.PortfolioMatching;

public sealed class GetPortfolioCompanyUsageQueryHandler(
    MarketIntelligenceDbContext dbContext)
    : IQueryHandler<
        GetPortfolioCompanyUsageQuery,
        IReadOnlyList<PortfolioCompanyUsageDto>>
{
    public async ValueTask<IReadOnlyList<PortfolioCompanyUsageDto>> Handle(
    GetPortfolioCompanyUsageQuery query,
    CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.FSortName);

        var positions =
            await dbContext.InvestmentPortfolioPositions
                .AsNoTracking()
                .Where(position =>
                    position.ChildCompanyId == null &&
                    position.FSortName == query.FSortName &&
                    position.IsListed == query.IsListed)
                .Select(position => new
                {
                    position.ParentCompanyId,
                    position.PeriodEndDate,
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        int[] parentCompanyIds =
            positions
                .Select(position => position.ParentCompanyId)
                .Distinct()
                .ToArray();

        var parentCompanies =
            await dbContext.CompanyMaster
                .AsNoTracking()
                .Where(company =>
                    parentCompanyIds.Contains(company.CompanyId))
                .Select(company => new
                {
                    company.CompanyId,
                    company.Symbol,
                    company.CompanyName,
                })
                .ToDictionaryAsync(
                    company => company.CompanyId,
                    cancellationToken)
                .ConfigureAwait(false);

        IReadOnlyList<PortfolioCompanyUsageDto> result =
            positions
                .GroupBy(position => position.ParentCompanyId)
                .Select(group =>
                {
                    parentCompanies.TryGetValue(
                        group.Key,
                        out var parentCompany);

                    return new PortfolioCompanyUsageDto(
                        parentCompany?.Symbol ??
                            group.Key.ToString(
                                System.Globalization.CultureInfo.InvariantCulture),
                        parentCompany?.CompanyName ?? "—",
                        group.Count(),
                        group.Select(position => position.PeriodEndDate)
                            .OrderBy(period => period, StringComparer.Ordinal)
                            .First(),
                        group.Select(position => position.PeriodEndDate)
                            .OrderByDescending(period => period, StringComparer.Ordinal)
                            .First());
                })
                .OrderByDescending(item => item.OccurrenceCount)
                .ThenBy(item => item.Symbol)
                .ToList();

        return result;
    }
}