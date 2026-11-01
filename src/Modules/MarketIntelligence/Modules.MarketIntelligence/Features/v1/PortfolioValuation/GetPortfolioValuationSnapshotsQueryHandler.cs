using FSH.Framework.Shared.Persistence;
using FSH.Modules.MarketIntelligence.Contracts.Dtos;
using FSH.Modules.MarketIntelligence.Contracts.v1.PortfolioValuation;
using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Services.Subscriptions;
using Mediator;
using Microsoft.EntityFrameworkCore;
using static FSH.Framework.BuildingBlocks.Shared.Globalization.PersianTextNormalizer;

namespace FSH.Modules.MarketIntelligence.Features.v1.PortfolioValuation;

public sealed class GetPortfolioValuationSnapshotsQueryHandler(
    MarketIntelligenceDbContext dbContext,
    LatestDataAccessService latestDataAccessService)
    : IQueryHandler<
        GetPortfolioValuationSnapshotsQuery,
        PagedResponse<PortfolioValuationItemDto>>
{
    public async ValueTask<PagedResponse<PortfolioValuationItemDto>> Handle(
        GetPortfolioValuationSnapshotsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = Math.Max(1, query.PageNumber);
        int size = query.PageSize is < 1 or > 200
            ? 20
            : query.PageSize;

        var accessScope = await latestDataAccessService
            .GetScopeAsync(cancellationToken);

        var snapshots = await dbContext.PortfolioValuationSnapshots
            .AsNoTracking()
            .Where(x => x.FrozenAt == null)
            .ToListAsync(cancellationToken);

        var companyRows = await dbContext.CompanyMaster
            .AsNoTracking()
            .Select(x => new
            {
                x.CompanyId,
                x.Symbol,
                x.CompanyName
            })
            .ToListAsync(cancellationToken);

        var companies = companyRows
            .ToDictionary(x => x.CompanyId);

        var items = new List<PortfolioValuationItemDto>();

        string? search = string.IsNullOrWhiteSpace(query.Search)
            ? null
            : NormalizeForMatch(query.Search);

        foreach (var snapshot in snapshots)
        {
            bool latest = accessScope.CanAccessLatest(
                snapshot.CompanyId);

            if (!latest &&
                (!snapshot.PreviousDisclosureId.HasValue ||
                 string.IsNullOrWhiteSpace(
                     snapshot.PreviousPeriodEndDate)))
            {
                continue;
            }

            companies.TryGetValue(
                snapshot.CompanyId,
                out var company);

            if (search is not null &&
                !(company?.Symbol is string symbol &&
                NormalizeForMatch(symbol).Contains(
                 search, StringComparison.OrdinalIgnoreCase)) &&
                !(company?.CompanyName is string companyName &&
                 NormalizeForMatch(companyName).Contains(
                 search, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            decimal? ratio = latest
                ? snapshot.CurrentPriceToPortfolioValuePercent
                : snapshot.PreviousPriceToPortfolioValuePercent;

            decimal? discount = ratio.HasValue
                ? Math.Round(100m - ratio.Value, 2)
                : null;

            items.Add(new PortfolioValuationItemDto(
                CompanyId: snapshot.CompanyId,
                Symbol: company?.Symbol,
                CompanyName: company?.CompanyName,

                DisclosureId: latest
                    ? snapshot.CurrentDisclosureId
                    : snapshot.PreviousDisclosureId!.Value,

                PeriodEndDate: latest
                    ? snapshot.PeriodEndDate
                    : snapshot.PreviousPeriodEndDate!,

                RegisteredCapital: latest
                    ? snapshot.CurrentRegisteredCapital
                    : snapshot.PreviousRegisteredCapital,

                CurrentListedValue: latest
                    ? snapshot.CurrentListedValue
                    : snapshot.PreviousListedValue,

                UnlistedReportedValue: latest
                    ? snapshot.CurrentUnlistedValue
                    : snapshot.PreviousUnlistedValue,

                CurrentPortfolioValue: latest
                    ? snapshot.CurrentPortfolioValue
                    : snapshot.PreviousPortfolioValue,

                CurrentPortfolioValuePerShare: latest
                    ? snapshot.CurrentValuePerShare
                    : snapshot.PreviousValuePerShare,

                CurrentSharePrice: latest
                    ? snapshot.CurrentSharePrice
                    : snapshot.PreviousSharePrice,

                CurrentSharePriceTradeDate: latest
                    ? snapshot.CurrentPriceTradeDate
                    : snapshot.PreviousPriceTradeDate,

                DiscountToPortfolioValuePercent: discount,

                ListedPositionCount: latest
                    ? snapshot.CurrentListedPositionCount
                    : snapshot.PreviousListedPositionCount ?? 0,

                PricedListedPositionCount: latest
                    ? snapshot.CurrentPricedPositionCount
                    : snapshot.PreviousPricedPositionCount ?? 0,

                IsHistorical: !latest));
        }

        bool ascending = string.Equals(
            query.SortDir,
            "asc",
            StringComparison.OrdinalIgnoreCase);

        string sortBy = query.SortBy?
            .Trim()
            .ToUpperInvariant() ?? "DISCOUNT";

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
            TotalPages = (int)Math.Ceiling(items.Count / (double)size),
            PageNumber = page,
            PageSize = size
        };
    }
}