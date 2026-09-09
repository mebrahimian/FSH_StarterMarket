using Mediator;

namespace FSH.Modules.MarketIntelligence.Contracts.v1.PortfolioViewer;

public sealed record GetPortfolioByDisclosureIdQuery(
    Guid DisclosureId)
    : IQuery<PortfolioReportDto?>;

public sealed record PortfolioReportDto(
    Guid DisclosureId,
    long TracingNo,
    int ParentCompanyId,
    string PeriodEndDate,
    DateTime? PublishDateTime,
    byte SourceType,
    byte AuditStatus,
    Guid? PreviousDisclosureId,
    Guid? NextDisclosureId,
    IReadOnlyList<PortfolioNavigationTargetDto> NavigationTargets,
    decimal? ListedReportedMarketValue,
    decimal? UnlistedReportedValue,
    IReadOnlyList<PortfolioPositionDto> Positions);

public sealed record PortfolioPositionDto(
    Guid Id,
    int RowSequence,
    int? ChildCompanyId,
    string RawCompanyName,
    string FSortName,
    bool IsListed,
    string? Symbol,
    string? CompanyName,
    decimal? Capital,
    decimal? NominalValue,
    decimal? BeginningQuantity,
    decimal? BeginningCost,
    decimal? BeginningMarketValue,
    decimal? ChangeQuantity,
    decimal? ChangeCost,
    decimal? ChangeMarketValue,
    decimal? OwnershipPercent,
    decimal? EndingQuantity,
    decimal? EndingCost,
    decimal? EndingMarketValue,
    decimal? EndingCostPerShare,
    decimal? EndingMarketPrice,
    decimal? IncreaseDecrease,
    string? Notes);

public sealed record PortfolioNavigationTargetDto(
    byte SourceType,
    byte AuditStatus,
    Guid DisclosureId,
    string PeriodEndDate);