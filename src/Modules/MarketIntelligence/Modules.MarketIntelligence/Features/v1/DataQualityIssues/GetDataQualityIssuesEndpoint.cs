using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.MarketIntelligence.Contracts.Authorization;
using FSH.Modules.MarketIntelligence.Contracts.v1.DataQualityIssues;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.MarketIntelligence.Features.v1.DataQualityIssues;

public static class GetDataQualityIssuesEndpoint
{
    internal static RouteHandlerBuilder MapGetDataQualityIssuesEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet(
                "/data-quality/issues",
                (
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(
                        new GetDataQualityIssuesQuery(),
                        cancellationToken))
            .WithName("GetDataQualityIssues")
            .WithSummary("Gets detected market intelligence data quality issues")
            .RequirePermission(
                MarketIntelligencePermissions.Disclosures.View);
    }
}