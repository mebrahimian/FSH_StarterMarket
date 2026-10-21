using Mediator;

namespace FSH.Modules.MarketIntelligence.Features.v1.UserSubscriptions;

public sealed record GetMySubscriptionQuery
    : IQuery<GetMySubscriptionResponse>;

public sealed record GetMySubscriptionResponse(
    string PlanKey,
    int MaxSelectedSymbols,
    int MaxSymbolsPerIndustry,
    int SelectedSymbolsCount,
    DateTimeOffset? StartUtc,
    DateTimeOffset? EndUtc,
    bool IsFree);