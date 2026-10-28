using FSH.Framework.Shared.Persistence;
using FSH.Modules.MarketIntelligence.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.MarketIntelligence.Contracts.v1.PortfolioValuation;

public sealed record GetPortfolioValuationQuery(
    string? Search = null,
    int PageNumber = 1,
    int PageSize = 20,
    string? SortBy = null,
    string? SortDir = null)
    : IQuery<PagedResponse<PortfolioValuationItemDto>>;