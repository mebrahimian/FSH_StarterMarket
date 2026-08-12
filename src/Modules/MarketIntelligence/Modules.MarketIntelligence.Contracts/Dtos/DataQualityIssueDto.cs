namespace FSH.Modules.MarketIntelligence.Contracts.Dtos;

public sealed record DataQualityIssueDto(
    string Symbol,
    string? YearEndDate,
    string PeriodEndDate,
    string? PublishDate,
    string IssueCode,
    decimal? PreviousValue,
    decimal? CurrentValue);