namespace FSH.Modules.MarketIntelligence.Services.Codal.DataQuality;

public sealed class CodalHistoryCoverageQuality
{
    public int CoverageYears { get; init; }

    public int RequiredMonths { get; init; }

    public string? WindowStartPeriod { get; init; }

    public string? WindowEndPeriod { get; init; }

    public int ActiveSymbols { get; init; }

    public int CompleteSymbols { get; init; }

    public int IncompleteSymbols { get; init; }

    public IReadOnlyList<CodalSymbolCoverageGap> Gaps
    {
        get;
        init;
    } = [];
}

public sealed class CodalSymbolCoverageGap
{
    public required string Symbol { get; init; }

    public int AvailableMonths { get; init; }

    public int MissingMonths { get; init; }

    public string? OldestAvailablePeriod { get; init; }

    public string? NewestAvailablePeriod { get; init; }

    public IReadOnlyList<string> MissingPeriods
    {
        get;
        init;
    } = [];
}