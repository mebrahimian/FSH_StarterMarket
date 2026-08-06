using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.MarketIntelligence.Contracts.Dtos;

using FSH.Modules.MarketIntelligence.Contracts.Authorization;
using FSH.Modules.MarketIntelligence.Contracts.v1.Disclosures;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.MarketIntelligence.Features.v1.Disclosures.SearchDisclosures;

public static class SearchDisclosuresEndpoint
{
    internal static RouteHandlerBuilder MapSearchDisclosuresEndpoint(
    this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet(
                "/disclosures",
                (
                    string? search,
                    short? let,
                    short[]? lets,
                    bool? includeNullLet,
                    byte? rt,
                    int? reportingTypeCode,
                    string? salesParseStatus,
                    int pageNumber,
                    int pageSize,
                    string? sortBy,
                    string? sortDir,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(
                        new SearchDisclosuresQuery(
                            Search: search,
                            Let: let,
                            Lets: lets,
                            IncludeNullLet: includeNullLet == true,
                            Rt: rt,
                            ReportingTypeCode: reportingTypeCode,
                            SalesParseStatus: salesParseStatus,
                            PageNumber: pageNumber == 0 ? 1 : pageNumber,
                            PageSize: pageSize == 0 ? 20 : pageSize,
                            SortBy: sortBy,
                            SortDir: sortDir),
                        cancellationToken))
            .WithName("SearchDisclosures")
            .WithSummary("Search Codal disclosures with filtering, pagination and sorting")
            .RequirePermission(
                MarketIntelligencePermissions.Disclosures.View);
    }
}
