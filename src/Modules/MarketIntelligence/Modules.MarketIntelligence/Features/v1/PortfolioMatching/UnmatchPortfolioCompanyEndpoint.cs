using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.MarketIntelligence.Contracts.Authorization;
using FSH.Modules.MarketIntelligence.Contracts.v1.PortfolioMatching;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.MarketIntelligence.Features.v1.PortfolioMatching;

public static class UnmatchPortfolioCompanyEndpoint
{
    internal static RouteHandlerBuilder MapUnmatchPortfolioCompanyEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost(
                "/portfolio-matching/unmatch",
                (
                    UnmatchPortfolioCompanyCommand command,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(
                        command,
                        cancellationToken))
            .WithName("UnmatchPortfolioCompany")
            .WithSummary(
                "Removes an investment portfolio company match")
            .RequirePermission(
                MarketIntelligencePermissions.Disclosures.Update);
    }
}