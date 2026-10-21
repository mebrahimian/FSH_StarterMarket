using FSH.Framework.Core.Context;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.MarketIntelligence.Contracts.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.MarketIntelligence.Features.v1.UserSubscriptions;

public static class ApproveUserPaymentEndpoint
{
    internal static RouteHandlerBuilder MapApproveUserPaymentEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost(
    "/subscriptions/payment-requests/{id:guid}/approve",
    async (
        Guid id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)]
        ApproveUserPaymentRequestBody? request,
        ICurrentUser currentUser,
        IMediator mediator,
        CancellationToken cancellationToken) =>
    {
        if (currentUser.GetTenant() != "marketintelligence")
            return Results.NotFound();

        await mediator.Send(
            new ApproveUserPaymentCommand(id, request?.PaidAmount),
            cancellationToken);

        return Results.NoContent();
    })
.WithName("ApproveMarketIntelligencePayment")
.WithSummary("Approves a payment and activates the user's subscription")
.RequirePermission(MarketIntelligencePermissions.Payments.Review);
}
public sealed record ApproveUserPaymentRequestBody(
        decimal? PaidAmount);
}