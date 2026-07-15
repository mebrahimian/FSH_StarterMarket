using FSH.Framework.Shared.Persistence;
using FSH.Modules.MarketIntelligence.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.MarketIntelligence.Contracts.v1.Disclosures;

/// <summary>
/// Search for brands with pagination and sorting.
/// </summary>
/// <param name="Search">Search term.</param>
/// <param name="PageNumber">Page number.</param>
/// <param name="PageSize">Page size.</param>
/// <param name="SortBy">Sort column. One of: name | slug | createdAtUtc.</param>
/// <param name="SortDir">Sort direction. One of: asc | desc.</param>
public sealed record SearchDisclosuresQuery(
    string? Search = null,
    int PageNumber = 1,
    int PageSize = 20,
    string? SortBy = null,
    string? SortDir = null) : IQuery<PagedResponse<DisclosureDto>>;
