using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.MarketIntelligence.Contracts.Authorization;
using FSH.Modules.MarketIntelligence.Contracts.v1.PortfolioMatching;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.MarketIntelligence.Features.v1.PortfolioMatching;

public static class GetMatchedPortfolioCompaniesEndpoint
{
    internal static RouteHandlerBuilder MapGetMatchedPortfolioCompaniesEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet(
                "/portfolio-matching/matched",
                (
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(
                        new GetMatchedPortfolioCompaniesQuery(),
                        cancellationToken))
            .WithName("GetMatchedPortfolioCompanies")
            .WithSummary(
                "Gets matched investment portfolio company names")
            .RequirePermission(
                MarketIntelligencePermissions.Disclosures.View);
    }
}