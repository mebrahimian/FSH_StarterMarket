using FSH.Framework.Core.Context;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.MarketIntelligence.Features.v1.UserSubscriptions;

public static class AttachPaymentReceiptEndpoint
{
    internal static RouteHandlerBuilder MapAttachPaymentReceiptEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost(
            "/subscriptions/payment-requests/{id:guid}/receipt",
            async (
                Guid id,
                AttachPaymentReceiptRequest request,
                ICurrentUser currentUser,
                IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.IsAuthenticated())
                    return Results.Unauthorized();

                if (currentUser.GetTenant() != "marketintelligence")
                    return Results.NotFound();

                await mediator.Send(
                    new AttachPaymentReceiptCommand(
                        id,
                        request.ReceiptFileId,
                        request.BankReferenceNumber),
                    cancellationToken);

                return Results.NoContent();
            })
            .WithName("AttachMarketIntelligencePaymentReceipt")
            .WithSummary("Attaches a receipt to the user's payment request")
            .RequireAuthorization();
    }
}

public sealed record AttachPaymentReceiptRequest(
    Guid ReceiptFileId,
    string? BankReferenceNumber);