using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.MarketIntelligence.Features.v1.UserSubscriptions;

public static class GetSubscriptionPlansEndpoint
{
    internal static RouteHandlerBuilder MapGetSubscriptionPlansEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet(
                "/subscriptions/plans",
                (
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(
                        new GetSubscriptionPlansQuery(),
                        cancellationToken))
            .WithName("GetMarketIntelligenceSubscriptionPlans")
            .WithSummary("Gets available subscription plans and discounted prices")
            .RequireAuthorization();
    }
}