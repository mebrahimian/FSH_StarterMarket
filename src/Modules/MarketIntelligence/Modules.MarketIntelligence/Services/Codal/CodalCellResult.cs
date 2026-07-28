namespace FSH.Modules.MarketIntelligence.Services.Codal;

public sealed record CodalCellResult(
    string? Value,
    string? Formula,
    string? PeriodEndToDate,
    string? YearEndToDate,
    string? Address,
    int RowSequence);