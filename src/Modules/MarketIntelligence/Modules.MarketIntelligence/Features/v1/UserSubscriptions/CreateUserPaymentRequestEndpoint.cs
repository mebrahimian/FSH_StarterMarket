using FSH.Framework.Core.Context;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.MarketIntelligence.Features.v1.UserSubscriptions;

public static class CreateUserPaymentRequestEndpoint
{
    internal static RouteHandlerBuilder MapCreateUserPaymentRequestEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost(
                "/subscriptions/payment-requests",
                async (
                    CreateUserPaymentRequestCommand command,
                    ICurrentUser currentUser,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                {
                    if (!currentUser.IsAuthenticated())
                        return Results.Unauthorized();

                    if (currentUser.GetTenant() != "marketintelligence")
                        return Results.NotFound();

                    var response = await mediator.Send(
                        command,
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("CreateMarketIntelligencePaymentRequest")
            .WithSummary("Creates a personal subscription payment request")
            .RequireAuthorization();
    }
}