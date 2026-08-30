using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.MarketIntelligence.Contracts.Authorization;
using FSH.Modules.MarketIntelligence.Contracts.v1.PortfolioMatching;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.MarketIntelligence.Features.v1.PortfolioMatching;

public static class GetUnmatchedPortfolioCompaniesEndpoint
{
    internal static RouteHandlerBuilder MapGetUnmatchedPortfolioCompaniesEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet(
                "/portfolio-matching/unmatched",
                (
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(
                        new GetUnmatchedPortfolioCompaniesQuery(),
                        cancellationToken))
            .WithName("GetUnmatchedPortfolioCompanies")
            .WithSummary(
                "Gets unmatched investment portfolio company names")
            .RequirePermission(
                MarketIntelligencePermissions.Disclosures.View);
    }
}