using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.MarketIntelligence.Contracts.Authorization;
using FSH.Modules.MarketIntelligence.Contracts.v1.PortfolioViewer;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.MarketIntelligence.Features.v1.PortfolioViewer;

public static class GetPortfolioByDisclosureIdEndpoint
{
    internal static RouteHandlerBuilder MapGetPortfolioByDisclosureIdEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet(
                "/portfolio-viewer/{disclosureId:guid}",
                (
                    Guid disclosureId,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(
                        new GetPortfolioByDisclosureIdQuery(
                            disclosureId),
                        cancellationToken))
            .WithName("GetPortfolioByDisclosureId")
            .WithSummary(
                "Gets investment portfolio positions for a disclosure")
            .RequirePermission(
                MarketIntelligencePermissions.Disclosures.View);
    }
}