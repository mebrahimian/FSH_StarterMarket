using FSH.Framework.Core.Context;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.MarketIntelligence.Features.v1.UserSubscriptions;

public static class CancelUserPaymentRequestEndpoint
{
    internal static RouteHandlerBuilder MapCancelUserPaymentRequestEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost(
            "/subscriptions/payment-requests/{id:guid}/cancel",
            async (
                Guid id,
                CancelUserPaymentRequestRequest request,
                ICurrentUser currentUser,
                IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.IsAuthenticated())
                    return Results.Unauthorized();

                if (currentUser.GetTenant() != "marketintelligence")
                    return Results.NotFound();

                await mediator.Send(
                    new CancelUserPaymentRequestCommand(
                        id,
                        request.ConfirmedNoPayment),
                    cancellationToken);

                return Results.NoContent();
            })
            .WithName("CancelMarketIntelligencePaymentRequest")
            .WithSummary("Cancels the user's pending payment request")
            .RequireAuthorization();
    }
}

public sealed record CancelUserPaymentRequestRequest(
    bool ConfirmedNoPayment);