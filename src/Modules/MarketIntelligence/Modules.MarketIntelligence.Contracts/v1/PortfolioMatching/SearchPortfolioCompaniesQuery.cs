using Mediator;

namespace FSH.Modules.MarketIntelligence.Contracts.v1.PortfolioMatching;

public sealed record PortfolioCompanyTargetDto(
    int CompanyId,
    string? Symbol,
    string CompanyName,
    bool IsListed);

public sealed record SearchPortfolioCompaniesQuery(
    string SearchText,
    bool IsListed)
    : IQuery<IReadOnlyList<PortfolioCompanyTargetDto>>;