
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.MarketIntelligence.Contracts.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.MarketIntelligence.Features.v1.RealMoneyFlow;

public static class GetRealMoneyFlowSignalEndpoint
{
    internal static RouteHandlerBuilder MapGetRealMoneyFlowSignalEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet(
                "/real-money-flow/{companyId:int}",
                async (
                    int companyId,
                    DateOnly? asOfDate,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                {
                    if (companyId <= 0)
                        return Results.BadRequest("Invalid companyId.");

                    var result = await mediator.Send(
                        new GetRealMoneyFlowSignalQuery(
                            companyId,
                            asOfDate),
                        cancellationToken);

                    return result is null
                        ? Results.NotFound()
                        : Results.Ok(result);
                })
            .WithName("GetRealMoneyFlowSignal")
            .WithSummary(
                "Gets real money flow signal by CompanyId")
            .RequirePermission(
                MarketIntelligencePermissions.Disclosures.View);
    }
}
