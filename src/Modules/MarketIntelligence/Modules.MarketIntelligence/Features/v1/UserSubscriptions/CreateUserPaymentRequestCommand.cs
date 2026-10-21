using Mediator;
using FSH.Modules.MarketIntelligence.Domain.Enums;

namespace FSH.Modules.MarketIntelligence.Features.v1.UserSubscriptions;

public sealed record CreateUserPaymentRequestCommand(
    string PlanKey,
    int DurationMonths,
    UserPaymentRequestType RequestType = UserPaymentRequestType.Purchase
) : ICommand<Guid>;