using Mediator;

namespace FSH.Modules.MarketIntelligence.Features.v1.UserSubscriptions;

public sealed record AttachPaymentReceiptCommand(
    Guid PaymentRequestId,
    Guid ReceiptFileId,
    string? BankReferenceNumber
) : ICommand;