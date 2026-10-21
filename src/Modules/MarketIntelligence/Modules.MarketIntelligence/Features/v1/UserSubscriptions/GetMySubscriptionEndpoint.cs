using FSH.Framework.Core.Context;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.MarketIntelligence.Features.v1.UserSubscriptions;

public static class GetMySubscriptionEndpoint
{
    internal static RouteHandlerBuilder MapGetMySubscriptionEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet(
                "/subscriptions/me",
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
                        new GetMySubscriptionQuery(),
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("GetMyMarketIntelligenceSubscription")
            .WithSummary("Gets the current user's Market Intelligence subscription")
            .RequireAuthorization();
    }
}