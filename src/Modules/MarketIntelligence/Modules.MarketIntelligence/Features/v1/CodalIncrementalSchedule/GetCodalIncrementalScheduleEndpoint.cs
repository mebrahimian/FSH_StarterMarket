using FSH.Modules.MarketIntelligence.Contracts.Authorization;
using FSH.Modules.MarketIntelligence.Contracts.v1.CodalIncrementalSchedule;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using FSH.Framework.Shared.Identity.Authorization;

namespace FSH.Modules.MarketIntelligence.Features.v1.CodalIncrementalSchedule;

public static class GetCodalIncrementalScheduleEndpoint
{
    internal static RouteHandlerBuilder MapGetCodalIncrementalScheduleEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet(
                "/codal/incremental-schedule",
                (
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(
                        new GetCodalIncrementalScheduleQuery(),
                        cancellationToken))
            .WithName("GetCodalIncrementalSchedule")
            .WithSummary("Gets Codal incremental collection schedule settings")
            .RequirePermission(
                MarketIntelligencePermissions.Disclosures.View);
    }
}