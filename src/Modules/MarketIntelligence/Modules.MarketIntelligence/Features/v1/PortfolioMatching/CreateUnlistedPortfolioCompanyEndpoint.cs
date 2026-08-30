using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.MarketIntelligence.Contracts.Authorization;
using FSH.Modules.MarketIntelligence.Contracts.v1.PortfolioMatching;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.MarketIntelligence.Features.v1.PortfolioMatching;

public static class CreateUnlistedPortfolioCompanyEndpoint
{
    internal static RouteHandlerBuilder MapCreateUnlistedPortfolioCompanyEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost(
                "/portfolio-matching/unlisted-companies",
                (
                    CreateUnlistedPortfolioCompanyCommand command,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(
                        command,
                        cancellationToken))
            .WithName("CreateUnlistedPortfolioCompany")
            .WithSummary(
                "Creates a new unlisted company for portfolio matching")
            .RequirePermission(
                MarketIntelligencePermissions.Disclosures.Update);
    }
}