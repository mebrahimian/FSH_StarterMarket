namespace FSH.Modules.MarketIntelligence.Contracts.Dtos;

public sealed record FiscalYearSalesDto(
    string Symbol,
    string YearEndDate,
    IReadOnlyList<FiscalYearSalesRowDto> Rows);

public sealed record FiscalYearSalesRowDto(
    string PeriodEndDate,
    decimal? PeriodAmount,
    decimal? YearToDateAmount,
    decimal? PreviousYearToDateAmount,
    bool IsMissing);