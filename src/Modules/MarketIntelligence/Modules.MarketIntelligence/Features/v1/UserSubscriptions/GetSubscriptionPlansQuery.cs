using FSH.Modules.Billing.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.MarketIntelligence.Features.v1.UserSubscriptions;

public sealed record GetSubscriptionPlansQuery
    : IQuery<IReadOnlyList<SubscriptionPlanResponse>>;

public sealed record SubscriptionTermResponse(
    int DurationMonths,
    decimal DiscountPercent,
    decimal FinalPrice);

public sealed record SubscriptionPlanResponse(
    string PlanKey,
    string Currency,
    decimal MonthlyBasePrice,
    IReadOnlyList<SubscriptionTermResponse> Terms);