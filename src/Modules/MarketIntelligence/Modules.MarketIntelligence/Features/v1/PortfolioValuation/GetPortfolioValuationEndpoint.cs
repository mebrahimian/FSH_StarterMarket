using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.MarketIntelligence.Contracts.Authorization;
using FSH.Modules.MarketIntelligence.Contracts.v1.PortfolioValuation;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.MarketIntelligence.Features.v1.PortfolioValuation;

public static class GetPortfolioValuationEndpoint
{
    internal static RouteHandlerBuilder MapGetPortfolioValuationEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet(
                "/portfolio-valuation",
                (
                    string? search,
                    int? pageNumber,
                    int? pageSize,
                    string? sortBy,
                    string? sortDir,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(
                        new GetPortfolioValuationQuery(
                            Search: search,
                            PageNumber: pageNumber ?? 1,
                            PageSize: pageSize ?? 20,
                            SortBy: sortBy,
                            SortDir: sortDir),
                        cancellationToken))
            .WithName("GetPortfolioValuation")
            .WithSummary(
                "Gets the latest portfolio valuations for investment companies")
            .RequirePermission(
                MarketIntelligencePermissions.Disclosures.View);
    }
}