using Mediator;

namespace FSH.Modules.MarketIntelligence.Features.v1.UserSubscriptions;

public sealed record GetMySubscriptionSelectionsQuery
    : IQuery<GetMySubscriptionSelectionsResponse>;

public sealed record GetMySubscriptionSelectionsResponse(
    string PlanKey,
    int MaxSelectedSymbols,
    int MaxPortfolioValuationSymbols,
    SubscriptionSelectedSymbolDto[] SelectedCompanyIds,
    SubscriptionSelectedSymbolDto[] SelectedPortfolioValuationCompanyIds);
public sealed record SubscriptionSelectedSymbolDto(
    int CompanyId,
    string? Symbol,
    string? CompanyName);