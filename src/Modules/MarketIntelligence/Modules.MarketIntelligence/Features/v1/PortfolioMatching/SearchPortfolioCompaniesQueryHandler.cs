using FSH.Framework.Shared.Utilities;
using FSH.Modules.MarketIntelligence.Contracts.v1.PortfolioMatching;
using FSH.Modules.MarketIntelligence.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Modules.MarketIntelligence.Features.v1.PortfolioMatching;

public sealed class SearchPortfolioCompaniesQueryHandler(
    MarketIntelligenceDbContext dbContext)
    : IQueryHandler<
        SearchPortfolioCompaniesQuery,
        IReadOnlyList<PortfolioCompanyTargetDto>>
{
    public async ValueTask<IReadOnlyList<PortfolioCompanyTargetDto>> Handle(
        SearchPortfolioCompaniesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        string searchText = query.SearchText.Trim();

        if (searchText.Length < 2)
        {
            return [];
        }

        string normalizedSearchText =
            FSort.Normalize(searchText);

        var companies =
            await dbContext.CompanyMaster
                .AsNoTracking()
                .Where(company =>
                    company.IsListed == query.IsListed &&
                    (
                        company.CompanyName.Contains(searchText) ||
                        company.FSortName.Contains(normalizedSearchText) ||
                        (company.Symbol != null &&
                         company.Symbol.Contains(searchText)) ||
                        (company.FSortSymbol != null &&
                         company.FSortSymbol.Contains(normalizedSearchText))
                    ))
                .Select(company =>
                    new PortfolioCompanyTargetDto(
                        company.CompanyId,
                        company.Symbol,
                        company.CompanyName,
                        company.IsListed))
                .Take(20)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        return companies;
    }
}