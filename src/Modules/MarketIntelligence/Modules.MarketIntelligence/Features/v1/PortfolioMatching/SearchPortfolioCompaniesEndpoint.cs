using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.MarketIntelligence.Contracts.Authorization;
using FSH.Modules.MarketIntelligence.Contracts.v1.PortfolioMatching;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.MarketIntelligence.Features.v1.PortfolioMatching;

public static class SearchPortfolioCompaniesEndpoint
{
    internal static RouteHandlerBuilder MapSearchPortfolioCompaniesEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet(
                "/portfolio-matching/companies",
                (
                    string searchText,
                    bool isListed,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(
                        new SearchPortfolioCompaniesQuery(
                            searchText,
                            isListed),
                        cancellationToken))
            .WithName("SearchPortfolioCompanies")
            .WithSummary(
                "Searches target companies for portfolio matching")
            .RequirePermission(
                MarketIntelligencePermissions.Disclosures.View);
    }
}