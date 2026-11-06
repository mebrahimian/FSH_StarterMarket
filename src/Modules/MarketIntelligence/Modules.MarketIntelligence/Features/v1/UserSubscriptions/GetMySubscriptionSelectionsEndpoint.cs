using FSH.Framework.Core.Context;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.MarketIntelligence.Features.v1.UserSubscriptions;

public static class GetMySubscriptionSelectionsEndpoint
{
    internal static RouteHandlerBuilder MapGetMySubscriptionSelectionsEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet("/subscriptions/me/selections",
                async (
                    ICurrentUser currentUser,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                {
                    if (!currentUser.IsAuthenticated())
                    {
                        return Results.Unauthorized();
                    }

                    if (currentUser.GetTenant() != "marketintelligence")
                    {
                        return Results.NotFound();
                    }

                    var response = await mediator.Send(
                        new GetMySubscriptionSelectionsQuery(),
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("GetMyMarketIntelligenceSubscriptionSelections")
            .WithSummary("Gets selected symbols and portfolio valuation limits")
            .RequireAuthorization();
    }
}