using FSH.Framework.Core.Context;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.MarketIntelligence.Contracts.Authorization;
using FSH.Modules.MarketIntelligence.Domain;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.MarketIntelligence.Features.v1.UserSubscriptions;

public static class GetPaymentRequestsEndpoint
{
    internal static RouteHandlerBuilder MapGetPaymentRequestsEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet(
                "/subscriptions/payment-requests",
                async (
                    UserPaymentStatus? status,
                    int? pageNumber,
                    int? pageSize,
                    ICurrentUser currentUser,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                {
                    if (!currentUser.IsAuthenticated())
                        return Results.Unauthorized();

                    if (currentUser.GetTenant() != "marketintelligence")
                        return Results.NotFound();

                    var response = await mediator.Send(
                        new GetPaymentRequestsQuery(
                            status,
                            pageNumber ?? 1,
                            pageSize ?? 20),
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("GetMarketIntelligencePaymentRequests")
            .WithSummary("Gets paginated payment requests for administrators")
            .RequirePermission(MarketIntelligencePermissions.Payments.Review);
    }
}