using FSH.Modules.MarketIntelligence.Contracts.Authorization;
using FSH.Modules.MarketIntelligence.Contracts.v1.PortfolioValuation;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using FSH.Framework.Shared.Identity.Authorization;

namespace FSH.Modules.MarketIntelligence.Features.v1.PortfolioValuation;

public static class GetPortfolioValuationSnapshotsEndpoint
{
    internal static RouteHandlerBuilder MapGetPortfolioValuationSnapshotsEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet("/portfolio-valuations",
                (
                    string? search,
                    int? pageNumber,
                    int? pageSize,
                    string? sortBy,
                    string? sortDir,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(
                        new GetPortfolioValuationSnapshotsQuery(
                            Search: search,
                            PageNumber: pageNumber ?? 1,
                            PageSize: pageSize ?? 20,
                            SortBy: sortBy,
                            SortDir: sortDir),
                        cancellationToken))
            .WithName("GetPortfolioValuationSnapshots")
            .WithSummary(
                "Gets portfolio valuations from persisted snapshots")
            .RequirePermission(
                MarketIntelligencePermissions.Disclosures.View);
    }
}