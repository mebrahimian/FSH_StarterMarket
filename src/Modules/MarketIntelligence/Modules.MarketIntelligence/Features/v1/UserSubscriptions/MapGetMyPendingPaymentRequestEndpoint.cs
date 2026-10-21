using FSH.Framework.Core.Context;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.MarketIntelligence.Features.v1.UserSubscriptions;

public static class GetMyPendingPaymentRequestEndpoint
{
    internal static RouteHandlerBuilder MapGetMyPendingPaymentRequestEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet(
                "/subscriptions/payment-requests/pending",
                async (
                    ICurrentUser currentUser,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                {
                    if (!currentUser.IsAuthenticated())
                        return Results.Unauthorized();

                    if (currentUser.GetTenant() != "marketintelligence")
                        return Results.NotFound();

                    var response = await mediator.Send(
                        new GetMyPendingPaymentRequestQuery(),
                        cancellationToken);

                    return response is null
                        ? Results.NoContent()
                        : Results.Ok(response);
                })
            .WithName("GetMyPendingMarketIntelligencePaymentRequest")
            .WithSummary("Gets the current user's pending payment request")
            .RequireAuthorization();
    }
}