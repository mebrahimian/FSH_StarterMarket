using Mediator;

namespace FSH.Modules.MarketIntelligence.Contracts.v1.PortfolioMatching;

public sealed record UnmatchedPortfolioCompanyDto(
    string RawCompanyName,
    string FSortName,
    bool IsListed,
    int OccurrenceCount,
    int ParentSymbolCount);
public sealed record MatchedPortfolioCompanyDto(
    string RawCompanyName,
    string FSortName,
    bool IsListed,
    int CompanyId,
    string? Symbol,
    string? CompanyName,
    int OccurrenceCount,
    int ParentSymbolCount,
    string FirstPeriod,
    string LastPeriod,
    bool HasAlias);
public sealed record PortfolioCompanyUsageDto(
    string Symbol,
    string CompanyName,
    int OccurrenceCount,
    string FirstPeriod,
    string LastPeriod);


public sealed record GetUnmatchedPortfolioCompaniesQuery
    : IQuery<IReadOnlyList<UnmatchedPortfolioCompanyDto>>;
public sealed record GetMatchedPortfolioCompaniesQuery
    : IQuery<IReadOnlyList<MatchedPortfolioCompanyDto>>;
public sealed record GetPortfolioCompanyUsageQuery(
    string FSortName,
    bool IsListed)
    : IQuery<IReadOnlyList<PortfolioCompanyUsageDto>>;