using Mediator;

namespace FSH.Modules.MarketIntelligence.Contracts.v1.PortfolioMatching;

public sealed record CreateUnlistedPortfolioCompanyCommand(
    string RawCompanyName,
    string FSortName)
    : ICommand<PortfolioCompanyTargetDto>;