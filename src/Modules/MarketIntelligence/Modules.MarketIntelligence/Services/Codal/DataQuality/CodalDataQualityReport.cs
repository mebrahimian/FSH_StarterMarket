namespace FSH.Modules.MarketIntelligence.Services.Codal.DataQuality;

public sealed class CodalDataQualityReport
{
    public DateTime CheckedAtUtc { get; init; }

    public required CodalMetadataQuality Metadata { get; init; }

    public required CodalMonthlyProcessingQuality MonthlyProcessing { get; init; }

    public required CodalSummaryQuality Summaries { get; init; }
    public CodalHistoryCoverageQuality HistoryCoverage
    {
        get;
        init;
    } = new();

}

public sealed class CodalMetadataQuality
{
    public int TotalDisclosures { get; init; }

    public int MissingSymbol { get; init; }

    public int MissingUrl { get; init; }

    public int MissingPublishDate { get; init; }

    public int MissingLet { get; init; }

    public int MissingRt { get; init; }
}

public sealed class CodalMonthlyProcessingQuality
{
    public int TotalCandidates { get; init; }

    public int Pending { get; init; }

    public int Success { get; init; }

    public int Failed { get; init; }

    public int NoData { get; init; }

    public int Skipped { get; init; }

    public int SuccessWithoutSummary { get; init; }
}

public sealed class CodalSummaryQuality
{
    public int TotalSummaries { get; init; }

    public int MissingSourceDisclosure { get; init; }

    public int SourceIdentityMismatch { get; init; }

    public int SourceSymbolMismatch { get; init; }

    public int SourcePublishDateMismatch { get; init; }

    public int SourceStatusNotSuccess { get; init; }

    public int DuplicateSymbolPeriods { get; init; }

    public int MissingPeriodAmount { get; init; }

    public int MissingYearToDateAmount { get; init; }

    public int MissingPreviousYearWhenHistoryExists { get; init; }
}