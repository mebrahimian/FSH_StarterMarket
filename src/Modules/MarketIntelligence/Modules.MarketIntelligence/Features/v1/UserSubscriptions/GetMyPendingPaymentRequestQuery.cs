using Mediator;

namespace FSH.Modules.MarketIntelligence.Features.v1.UserSubscriptions;

public sealed record GetMyPendingPaymentRequestQuery()
    : IQuery<MyPendingPaymentRequestResponse?>;

public sealed record MyPendingPaymentRequestResponse(
    Guid Id,
    string PlanKey,
    int DurationMonths,
    decimal OriginalPrice,
    decimal DiscountPercent,
    decimal Amount,
    string Currency,
    bool HasReceipt,
    DateTime CreatedAtUtc);