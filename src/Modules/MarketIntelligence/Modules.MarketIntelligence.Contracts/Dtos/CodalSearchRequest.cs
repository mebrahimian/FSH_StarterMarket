namespace FSH.Modules.MarketIntelligence.Contracts.Dtos;

public sealed class CodalSearchRequest
{
    public int? Category { get; init; } 

    public int PageNumber { get; init; } = 1;

    public bool Audited { get; init; } = true;

    public int AuditorRef { get; init; } = -1;

    public bool Childs { get; init; } = true;

    public int CompanyState { get; init; } = -1;

    public int CompanyType { get; init; } = -1;

    public bool Consolidatable { get; init; } = true;

    public bool IsNotAudited { get; init; } 

    public int Length { get; init; } = -1;

    public int LetterType { get; init; } = -1;

    public bool Mains { get; init; } = true;

    public bool NotAudited { get; init; } = true;

    public bool NotConsolidatable { get; init; } = true;

    public bool Publisher { get; init; } 

    public int ReportingType { get; init; } = -1;

    public long TracingNo { get; init; } = -1;
}