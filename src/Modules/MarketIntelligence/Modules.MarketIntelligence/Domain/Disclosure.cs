using FSH.Framework.Core.Domain;
using FSH.Framework.Shared.Persistence;

namespace FSH.Modules.MarketIntelligence.Domain;

/// <summary>
/// Represents a disclosure published by Codal.
/// This is a global market-data entity and is not tenant-specific.
/// </summary>
public sealed class Disclosure : BaseEntity<Guid>, IGlobalEntity
{
    /// <summary>
    /// Codal tracing number. This is the unique identifier of a disclosure.
    /// </summary>
    public long TracingNo { get; private set; }

    /// <summary>
    /// Stock symbol associated with the disclosure.
    /// </summary>
    public string Symbol { get; private set; } = string.Empty;

    /// <summary>
    /// Company name published by Codal.
    /// </summary>
    public string CompanyName { get; private set; } = string.Empty;

    /// <summary>
    /// Disclosure title.
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// Codal letter code.
    /// </summary>
    public string LetterCode { get; private set; } = string.Empty;

    /// <summary>
    /// Date and time when the disclosure was sent to Codal.
    /// </summary>
    public string? SentDateTimeRaw { get; set; }

    /// <summary>
    /// Date and time when the disclosure was published.
    /// </summary>
    public string? PublishDateTimeRaw { get;  set; }
    /// <summary>
    /// Date and time when the disclosure was sent to Codal.
    /// </summary>
    public DateTime? SentDateTime { get; private set; }

    /// <summary>
    /// Date and time when the disclosure was published.
    /// </summary>
    public DateTime? PublishDateTime { get; private set; }

    public bool HasHtml { get; private set; }

    public bool IsEstimate { get; private set; }

    public string Url { get; private set; } = string.Empty;

    public bool HasExcel { get; private set; }

    public bool HasPdf { get; private set; }

    public bool HasXbrl { get; private set; }

    public bool HasAttachment { get; private set; }

    public string? AttachmentUrl { get; private set; }

    public string? PdfUrl { get; private set; }

    public string? ExcelUrl { get; private set; }

    public string? XbrlUrl { get; private set; }

    public string? TedanUrl { get; private set; }

    private Disclosure()
    {
    }

    public Disclosure(
        long tracingNo,
        string symbol,
        string companyName,
        string title,
        string letterCode,
        string sentDateTimeRaw,
        string publishDateTimeRaw,
        DateTime? sentDateTime,
        DateTime? publishDateTime,
        bool hasHtml,
        bool isEstimate,
        string url,
        bool hasExcel,
        bool hasPdf,
        bool hasXbrl,
        bool hasAttachment,
        string? attachmentUrl,
        string? pdfUrl,
        string? excelUrl,
        string? xbrlUrl,
        string? tedanUrl)
    {
        TracingNo = tracingNo;
        Symbol = symbol;
        CompanyName = companyName;
        Title = title;
        LetterCode = letterCode;
        SentDateTimeRaw = sentDateTimeRaw;
        PublishDateTimeRaw = publishDateTimeRaw;
        SentDateTime = sentDateTime;
        PublishDateTime = publishDateTime;
        HasHtml = hasHtml;
        IsEstimate = isEstimate;
        Url = url;
        HasExcel = hasExcel;
        HasPdf = hasPdf;
        HasXbrl = hasXbrl;
        HasAttachment = hasAttachment;
        AttachmentUrl = attachmentUrl;
        PdfUrl = pdfUrl;
        ExcelUrl = excelUrl;
        XbrlUrl = xbrlUrl;
        TedanUrl = tedanUrl;
    }
}