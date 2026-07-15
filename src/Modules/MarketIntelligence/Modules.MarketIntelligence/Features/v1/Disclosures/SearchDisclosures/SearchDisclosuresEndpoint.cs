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
    internal static RouteHandlerBuilder MapSearchDisclosuresEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/disclosures",
                (string? search, int pageNumber, int pageSize, string? sortBy, string? sortDir,
                 IMediator mediator, CancellationToken ct) =>
                    mediator.Send(
                        new SearchDisclosuresQuery(
                            search,
                            pageNumber == 0 ? 1 : pageNumber,
                            pageSize == 0 ? 20 : pageSize,
                            sortBy,
                            sortDir),
                        ct))
            .WithName("SearchDisclosures")
            .WithSummary("Search disclosures (paged, sortable)")
            .RequirePermission(MarketIntelligencePermissions.Disclosures.View);
    }
}
