using FSH.Framework.Core.Context;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.MarketIntelligence.Features.v1.UserSubscriptions;

public static class SelectSubscriptionSymbolEndpoint
{
    internal static RouteHandlerBuilder MapSelectSubscriptionSymbolEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost(
                "/subscriptions/me/selections",
                async (
                    SelectSubscriptionSymbolCommand command,
                    ICurrentUser currentUser,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                {
                    if (!currentUser.IsAuthenticated())
                        return Results.Unauthorized();

                    if (currentUser.GetTenant() != "marketintelligence")
                        return Results.NotFound();

                    int companyId = await mediator.Send(
                        command,
                        cancellationToken);

                    return Results.Ok(new { CompanyId = companyId });
                })
            .WithName("SelectSubscriptionSymbol")
            .WithSummary("Permanently selects a symbol for a subscription feature")
            .RequireAuthorization();
    }
}