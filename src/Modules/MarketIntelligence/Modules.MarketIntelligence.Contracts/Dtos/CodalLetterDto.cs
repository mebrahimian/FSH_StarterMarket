using System.Text.Json.Serialization;

namespace FSH.Modules.MarketIntelligence.Contracts.Dtos;

public sealed class CodalLetterDto
{
    public long TracingNo { get; init; }

    public string? Symbol { get; init; }

    public string? CompanyName { get; init; }

    public string? Title { get; init; }

    public string? LetterCode { get; init; }
  
    [JsonPropertyName("SentDateTime")]
    public string? SentDateTimeRaw { get; init; }

    [JsonPropertyName("PublishDateTime")]
    public string? PublishDateTimeRaw { get; init; }

    public string? Url { get; init; }
    public bool HasHtml { get; init; }
    public bool HasExcel { get; init; }
    public bool HasPdf { get; init; }
    public bool HasXbrl { get; init; }
    public bool HasAttachment { get; init; }
    public string? AttachmentUrl { get; init; }
    public string? PdfUrl { get; init; }
    public string? ExcelUrl { get; init; }

    public string? XbrlUrl { get; init; }

    public string? TedanUrl { get; init; }

}