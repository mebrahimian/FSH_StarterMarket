using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.MarketIntelligence.Contracts.Authorization;
using FSH.Modules.MarketIntelligence.Contracts.v1.PortfolioMatching;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.MarketIntelligence.Features.v1.PortfolioMatching;

public static class MatchPortfolioCompanyEndpoint
{
    internal static RouteHandlerBuilder MapMatchPortfolioCompanyEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost(
                "/portfolio-matching/match",
                (
                    MatchPortfolioCompanyCommand command,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(
                        command,
                        cancellationToken))
            .WithName("MatchPortfolioCompany")
            .WithSummary(
                "Matches a portfolio company name to a company")
            .RequirePermission(
                MarketIntelligencePermissions.Disclosures.Update);
    }
}