using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.MarketIntelligence.Contracts.Authorization;
using FSH.Modules.MarketIntelligence.Contracts.v1.PortfolioMatching;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.MarketIntelligence.Features.v1.PortfolioMatching;

public static class GetPortfolioCompanyUsageEndpoint
{
    internal static RouteHandlerBuilder MapGetPortfolioCompanyUsageEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet(
                "/portfolio-matching/unmatched/usage",
                (
                    string fSortName,
                    bool isListed,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(
                        new GetPortfolioCompanyUsageQuery(
                            fSortName,
                            isListed),
                        cancellationToken))
            .WithName("GetPortfolioCompanyUsage")
            .WithSummary(
                "Gets symbols and periods using an unmatched portfolio company")
            .RequirePermission(
                MarketIntelligencePermissions.Disclosures.View);
    }
}