using FSH.Framework.Shared.Persistence;
using FSH.Modules.MarketIntelligence.Contracts.Dtos;
using FSH.Modules.MarketIntelligence.Contracts.v1.PortfolioValuation;
using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Services.MarketData;
using FSH.Modules.MarketIntelligence.Services.Portfolio;
using FSH.Modules.MarketIntelligence.Services.Subscriptions;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MarketIntelligence.Features.v1.PortfolioValuation;

public sealed class GetPortfolioValuationQueryHandler(
    MarketIntelligenceDbContext dbContext,
    IMarketPriceProvider marketPriceProvider,
    LatestDataAccessService latestDataAccessService)
    : IQueryHandler<
        GetPortfolioValuationQuery,
        PagedResponse<PortfolioValuationItemDto>>
{
    public async ValueTask<PagedResponse<PortfolioValuationItemDto>> Handle(
        GetPortfolioValuationQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = Math.Max(1, query.PageNumber);
        int size = query.PageSize is < 1 or > 200
            ? 20
            : query.PageSize;

        var accessScope = await latestDataAccessService
    .GetScopeAsync(cancellationToken);

        var reportHeaders = await dbContext.InvestmentPortfolioPositions
            .AsNoTracking()
            .Select(p => new
            {
                p.ParentCompanyId,
                p.DisclosureId,
                p.PeriodEndDate,
                p.PublishDateTime
            })
            .Distinct()
            .ToListAsync(cancellationToken);

        var latestReports = reportHeaders
            .GroupBy(p => p.ParentCompanyId)
            .Select(group =>
            {
                var ordered = group
                    .OrderByDescending(p => p.PeriodEndDate)
                    .ThenByDescending(p => p.PublishDateTime)
                    .ToList();

                var latest = ordered[0];

                bool canAccessLatest =
                    accessScope.AllCompanies ||
                    accessScope.CanAccessLatest(group.Key);

                if (canAccessLatest)
                {
                    return latest;
                }

                // Exclude the entire latest reporting period.
                // Use the newest older period available.
                return ordered.FirstOrDefault(p =>
                    p.PeriodEndDate != latest.PeriodEndDate);
            })
            .Where(p => p is not null)
            .Select(p => p!)
            .ToList();
        var companyRows = await dbContext.CompanyMaster
            .AsNoTracking()
            .Select(c => new
            {
                c.CompanyId,
                c.Symbol,
                c.CompanyName
            })
            .ToListAsync(cancellationToken);

        var companies = companyRows.ToDictionary(c => c.CompanyId);

        string? search = query.Search?.Trim();

        if (!string.IsNullOrWhiteSpace(search))
        {
            latestReports = latestReports
                .Where(report =>
                    companies.TryGetValue(
                        report.ParentCompanyId,
                        out var company) &&
                    (
                        (company.Symbol?.Contains(
                            search,
                            StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (company.CompanyName?.Contains(
                            search,
                            StringComparison.OrdinalIgnoreCase) ?? false)
                    ))
                .ToList();
        }

        if (latestReports.Count == 0)
        {
            return new PagedResponse<PortfolioValuationItemDto>
            {
                Items = [],
                TotalCount = 0,
                PageNumber = page,
                PageSize = size
            };
        }

        Guid[] disclosureIds = latestReports
            .Select(r => r.DisclosureId)
            .Distinct()
            .ToArray();

        var positions = await dbContext.InvestmentPortfolioPositions
            .AsNoTracking()
            .Where(p => disclosureIds.Contains(p.DisclosureId))
            .ToListAsync(cancellationToken);

        var positionsByDisclosure = positions
            .ToLookup(p => p.DisclosureId);

        var metadataRows = await dbContext.InvestmentPortfolioReportMetadata
            .AsNoTracking()
            .Where(m => disclosureIds.Contains(m.DisclosureId))
            .Select(m => new
            {
                m.DisclosureId,
                m.RegisteredCapital
            })
            .ToListAsync(cancellationToken);

        var capitalByDisclosure = metadataRows
            .GroupBy(m => m.DisclosureId)
            .ToDictionary(
                g => g.Key,
                g => g.FirstOrDefault(
                    m => m.RegisteredCapital.HasValue
                )?.RegisteredCapital);

        var currentReports = latestReports
    .Where(r =>
        accessScope.AllCompanies ||
        accessScope.CanAccessLatest(r.ParentCompanyId))
    .ToList();

        var currentDisclosureIds = currentReports
            .Select(r => r.DisclosureId)
            .ToHashSet();

        int[] priceCompanyIds = positions
            .Where(p =>
                currentDisclosureIds.Contains(p.DisclosureId) &&
                p.IsListed &&
                p.ChildCompanyId.HasValue)
            .Select(p => p.ChildCompanyId!.Value)
            .Concat(currentReports.Select(r => r.ParentCompanyId))
            .Distinct()
            .ToArray();

        IReadOnlyDictionary<int, MarketPriceSnapshot> prices =
            priceCompanyIds.Length > 0
                ? await marketPriceProvider.GetLatestPricesAsync(
                    priceCompanyIds,
                    cancellationToken)
                : new Dictionary<int, MarketPriceSnapshot>();

        var items = new List<PortfolioValuationItemDto>();

        foreach (var report in latestReports)
        {
            bool canAccessLatest =
                accessScope.AllCompanies ||
                accessScope.CanAccessLatest(report.ParentCompanyId);

            var reportPositions =
                positionsByDisclosure[report.DisclosureId].ToList();

            if (reportPositions.Count == 0)
            {
                continue;
            }

            decimal currentListedValue = 0m;
            decimal unlistedReportedValue = 0m;
            int listedCount = 0;
            int pricedListedCount = 0;

            foreach (var position in reportPositions)
            {
                MarketPriceSnapshot? marketPrice = null;

                if (position.IsListed &&
                    position.ChildCompanyId.HasValue)
                {
                    prices.TryGetValue(
                        position.ChildCompanyId.Value,
                        out marketPrice);
                }

                var valuation =
                    PortfolioPositionValuationCalculator.Calculate(
                        true,
                        position.IsListed,
                        position.EndingQuantity,
                        position.EndingMarketValue,
                        position.EndingCost,
                        marketPrice);

                if (position.IsListed)
                {
                    listedCount++;
                    currentListedValue += valuation.CurrentValue ?? 0m;

                    if (valuation.UsesCurrentMarketPrice)
                    {
                        pricedListedCount++;
                    }
                }
                else
                {
                    unlistedReportedValue +=
                        position.EndingCost ?? 0m;
                }
            }

            decimal portfolioValue =
                currentListedValue + unlistedReportedValue;

            capitalByDisclosure.TryGetValue(
                report.DisclosureId,
                out decimal? capital);

            decimal? portfolioValuePerShare =
                capital.HasValue && capital.Value > 0m
                    ? Math.Round(
                        portfolioValue / capital.Value * 1000m,
                        0)
                    : null;

            prices.TryGetValue(
                report.ParentCompanyId,
                out MarketPriceSnapshot? parentPrice);

            decimal? sharePrice =
                parentPrice?.LastPrice ??
                parentPrice?.ClosingPrice;

            decimal? discountPercent =
                portfolioValuePerShare > 0m &&
                sharePrice.HasValue
                    ? Math.Round(
                        (portfolioValuePerShare.Value -
                         sharePrice.Value) /
                        portfolioValuePerShare.Value * 100m,
                        2)
                    : null;

            companies.TryGetValue(
                report.ParentCompanyId,
                out var companyInfo);

            items.Add(new PortfolioValuationItemDto(
                CompanyId: report.ParentCompanyId,
                Symbol: companyInfo?.Symbol,
                CompanyName: companyInfo?.CompanyName,
                DisclosureId: report.DisclosureId,
                PeriodEndDate: report.PeriodEndDate,
                RegisteredCapital: capital,
                CurrentListedValue: currentListedValue,
                UnlistedReportedValue: unlistedReportedValue,
                CurrentPortfolioValue: portfolioValue,
                CurrentPortfolioValuePerShare: portfolioValuePerShare,
                CurrentSharePrice: sharePrice,
                CurrentSharePriceTradeDate: parentPrice?.TradeDate,
                DiscountToPortfolioValuePercent: discountPercent,
                ListedPositionCount: listedCount,
                PricedListedPositionCount: pricedListedCount,
                IsHistorical: !canAccessLatest));
        }

        bool ascending = string.Equals(
            query.SortDir, "asc",
            StringComparison.OrdinalIgnoreCase);

        string sortBy = query.SortBy?.Trim().ToUpperInvariant()
            ?? "DISCOUNT";

        IOrderedEnumerable<PortfolioValuationItemDto> ordered =
             sortBy switch
            {
                "SYMBOL" => ascending
                    ? items.OrderBy(x => x.Symbol)
                    : items.OrderByDescending(x => x.Symbol),

                "COMPANYNAME" => ascending
                    ? items.OrderBy(x => x.CompanyName)
                    : items.OrderByDescending(x => x.CompanyName),

                "CURRENTPORTFOLIOVALUE" => ascending
                    ? items.OrderBy(x => x.CurrentPortfolioValue)
                    : items.OrderByDescending(x => x.CurrentPortfolioValue),

                "CURRENTPORTFOLIOVALUEPERSHARE" => ascending
                    ? items.OrderBy(x => x.CurrentPortfolioValuePerShare)
                    : items.OrderByDescending(x => x.CurrentPortfolioValuePerShare),

                "CURRENTSHAREPRICE" => ascending
                    ? items.OrderBy(x => x.CurrentSharePrice)
                    : items.OrderByDescending(x => x.CurrentSharePrice),

                _ => ascending
                    ? items.OrderBy(x => x.DiscountToPortfolioValuePercent)
                    : items.OrderByDescending(x => x.DiscountToPortfolioValuePercent)
            };

        return new PagedResponse<PortfolioValuationItemDto>
        {
            Items = ordered
                .ThenBy(x => x.CompanyId)
                .Skip((page - 1) * size)
                .Take(size)
                .ToList(),
            TotalCount = items.Count,
            PageNumber = page,
            PageSize = size
        };
    }
}