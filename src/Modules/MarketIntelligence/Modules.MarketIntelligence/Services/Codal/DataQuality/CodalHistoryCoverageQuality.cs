namespace FSH.Modules.MarketIntelligence.Services.Codal.DataQuality;

public sealed class CodalHistoryCoverageQuality
{
    public IReadOnlyList<CodalSymbolCoverageGap> Gaps
    {
        get;
        init;
    } = [];

}
public sealed record CodalMissingPeriod(
    string PeriodEndDate,
    string? PublishDate);
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
    public IReadOnlyList<CodalMissingPeriod> MissingPeriodDetails
    {
        get;
        init;
    } = [];
}