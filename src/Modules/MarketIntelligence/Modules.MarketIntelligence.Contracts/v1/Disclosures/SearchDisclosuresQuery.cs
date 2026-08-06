using FSH.Framework.Shared.Persistence;
using FSH.Modules.MarketIntelligence.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.MarketIntelligence.Contracts.v1.Disclosures;

/// <summary>
/// Searches Codal disclosures with filtering, pagination and sorting.
/// </summary>
/// <param name="Search">
/// Search term applied to symbol, company name, title and letter code.
/// </param>
/// <param name="Let">Optional Codal letter type filter.</param>
/// <param name="Rt">Optional Codal report type filter.</param>
/// <param name="ReportingTypeCode">
/// Optional Codal reporting type code filter.
/// </param>
/// <param name="SalesParseStatus">
/// Optional parse status: Pending, Success, Failed, NoData or Skipped.
/// </param>
/// <param name="PageNumber">One-based page number.</param>
/// <param name="PageSize">Number of records per page.</param>
/// <param name="SortBy">
/// Sort column: tracingNo, symbol, companyName, publishDateTime,
/// sentDateTime or salesParsedAt.
/// </param>
/// <param name="SortDir">Sort direction: asc or desc.</param>
/// /// <param name="Lets">
/// LET codes used for grouped filtering.
/// </param>
/// <param name="IncludeNullLet">
/// Whether disclosures with a null LET value should be included.
/// </param>
public sealed record SearchDisclosuresQuery(
    string? Search = null,
    short? Let = null,
    short[]? Lets = null,
    bool IncludeNullLet = false,
    byte? Rt = null,
    int? ReportingTypeCode = null,
    string? SalesParseStatus = null,
    int PageNumber = 1,
    int PageSize = 20,
    string? SortBy = null,
    string? SortDir = null)
    : IQuery<PagedResponse<DisclosureDto>>;