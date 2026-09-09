using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.MarketIntelligence.Contracts.Authorization;
using FSH.Modules.MarketIntelligence.Contracts.v1.CodalIncrementalSchedule;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.MarketIntelligence.Features.v1.CodalIncrementalSchedule;

public static class UpdateCodalIncrementalScheduleEndpoint
{
    internal static RouteHandlerBuilder MapUpdateCodalIncrementalScheduleEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPut(
                "/codal/incremental-schedule",
                (
                    UpdateCodalIncrementalScheduleCommand command,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(
                        command,
                        cancellationToken))
            .WithName("UpdateCodalIncrementalSchedule")
            .WithSummary("Updates Codal incremental collection schedule settings")
            .RequirePermission(
                MarketIntelligencePermissions.Disclosures.Update);
    }
}