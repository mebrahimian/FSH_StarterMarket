using Mediator;

namespace FSH.Modules.MarketIntelligence.Features.v1.UserSubscriptions;

public enum SubscriptionSymbolPurpose
{
    None = 0,
    LatestData = 1,
    PortfolioValuation = 2
}

public sealed record SelectSubscriptionSymbolCommand(
    int CompanyId,
    SubscriptionSymbolPurpose Purpose)
    : ICommand<int>;