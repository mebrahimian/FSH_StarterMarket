using Mediator;

namespace FSH.Modules.MarketIntelligence.Contracts.v1.PortfolioMatching;

public sealed record UnmatchPortfolioCompanyCommand(
    string FSortName,
    bool IsListed,
    int CompanyId)
    : ICommand<int>;