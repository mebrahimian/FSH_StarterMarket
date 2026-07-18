using System.Text.Json.Serialization;

namespace FSH.Modules.MarketIntelligence.Contracts.Dtos;

public sealed class CodalLetterDto
{
    public long TracingNo { get; init; }

    public string? Symbol { get; init; }

    public string? CompanyName { get; init; }

    public string? Title { get; init; }

    public string? LetterCode { get; init; }

    public string? SentDateTime { get; init; }

    public string? PublishDateTime { get; init; }

    public string? Url { get; init; }

    public bool HasHtml { get; init; }

    public bool HasExcel { get; init; }
}