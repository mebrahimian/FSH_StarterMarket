using Mediator;

namespace FSH.Modules.MarketIntelligence.Features.v1.UserSubscriptions;

public sealed record ApproveUserPaymentCommand(
    Guid PaymentRequestId,
    decimal? PaidAmount = null
) : ICommand;