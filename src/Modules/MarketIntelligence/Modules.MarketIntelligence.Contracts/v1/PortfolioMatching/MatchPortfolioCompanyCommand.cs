using Mediator;

namespace FSH.Modules.MarketIntelligence.Contracts.v1.PortfolioMatching;

public sealed record MatchPortfolioCompanyCommand(
    string RawCompanyName,
    string FSortName,
    bool IsListed,
    int CompanyId)
    : ICommand<int>;