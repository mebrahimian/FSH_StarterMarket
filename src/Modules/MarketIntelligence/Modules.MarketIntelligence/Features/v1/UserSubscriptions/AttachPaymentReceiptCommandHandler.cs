using FSH.Framework.Core.Context;
using FSH.Modules.Files.Contracts.v1.DTOs;
using FSH.Modules.Files.Contracts.v1.Queries;
using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MarketIntelligence.Features.v1.UserSubscriptions;

public sealed class AttachPaymentReceiptCommandHandler(
    MarketIntelligenceDbContext dbContext,
    IMediator mediator,
    ICurrentUser currentUser)
    : ICommandHandler<AttachPaymentReceiptCommand>
{
    public async ValueTask<Unit> Handle(
        AttachPaymentReceiptCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!currentUser.IsAuthenticated() ||
            currentUser.GetTenant() != "marketintelligence")
        {
            throw new UnauthorizedAccessException();
        }

        var tenantId = currentUser.GetTenant();
        var userId = currentUser.GetUserId().ToString();

        var request = await dbContext.UserPaymentRequests
            .SingleOrDefaultAsync(x =>
                x.Id == command.PaymentRequestId &&
                x.TenantId == tenantId &&
                x.UserId == userId,
                cancellationToken);

        if (request is null)
            throw new InvalidOperationException("Payment request not found.");

        if (request.Status != UserPaymentStatus.Pending)
            throw new InvalidOperationException("Payment request is not pending.");

        var file = await mediator.Send(
            new GetFileMetadataQuery(command.ReceiptFileId),
            cancellationToken);

        if (file.Id != command.ReceiptFileId ||
            file.OwnerType != "UserPaymentRequest" ||
            file.OwnerId != request.Id ||
            file.CreatedByUserId != userId ||
            file.Status != FileAssetStatus.Available ||
            file.Visibility != Visibility.Private)
        {
            throw new InvalidOperationException("Invalid payment receipt.");
        }

        if (file.ContentType is not
            ("application/pdf" or "image/jpeg" or "image/png"))
        {
            throw new InvalidOperationException("Unsupported receipt format.");
        }

        request.AttachReceipt(
            command.ReceiptFileId,
            command.BankReferenceNumber);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}