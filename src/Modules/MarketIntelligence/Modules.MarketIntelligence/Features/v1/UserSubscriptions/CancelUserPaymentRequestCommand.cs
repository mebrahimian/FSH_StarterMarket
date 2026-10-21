using Mediator;

namespace FSH.Modules.MarketIntelligence.Features.v1.UserSubscriptions;

public sealed record CancelUserPaymentRequestCommand(
    Guid PaymentRequestId,
    bool ConfirmedNoPayment
) : ICommand;