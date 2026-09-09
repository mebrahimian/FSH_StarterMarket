using FSH.Framework.Core.Domain;
using FSH.Modules.MarketIntelligence.Domain.Enums;

namespace FSH.Modules.MarketIntelligence.Domain;

public sealed class InvestmentPortfolioReportMetadata :
    BaseEntity<Guid>,
    IGlobalEntity
{
    private InvestmentPortfolioReportMetadata()
    {
    }

    public InvestmentPortfolioReportMetadata(
    Guid disclosureId,
    long tracingNo,
    string? periodEndToDate,
    string? yearEndToDate,
    string? period,
    string? type,
    int sheetCode,
    int metaTableId,
    int metaTableCode,
    string? titleFa,
    string? titleEn,
    PortfolioSourceType sourceType,
    PortfolioAuditStatus auditStatus,
    string? reportSymbol = null,
    string? reportCompanyName = null,
    decimal? registeredCapital = null,
    decimal? unauthorizedCapital = null)
    {
        DisclosureId = disclosureId;
        TracingNo = tracingNo;
        PeriodEndToDate = periodEndToDate;
        YearEndToDate = yearEndToDate;
        Period = period;
        Type = type;
        SheetCode = sheetCode;
        MetaTableId = metaTableId;
        MetaTableCode = metaTableCode;
        TitleFa = titleFa;
        TitleEn = titleEn;
        SourceType = sourceType;
        AuditStatus = auditStatus;
        ReportSymbol = reportSymbol;
        ReportCompanyName = reportCompanyName;
        RegisteredCapital = registeredCapital;
        UnauthorizedCapital = unauthorizedCapital;
        ParsedAt = DateTime.UtcNow;
    }
    public void UpdateReportHeader(
    string? reportSymbol,
    string? reportCompanyName,
    decimal? registeredCapital,
    decimal? unauthorizedCapital)
    {
        if (!string.IsNullOrWhiteSpace(reportSymbol))
        {
            ReportSymbol = reportSymbol;
        }

        if (!string.IsNullOrWhiteSpace(reportCompanyName))
        {
            ReportCompanyName = reportCompanyName;
        }

        if (registeredCapital.HasValue)
        {
            RegisteredCapital = registeredCapital;
        }

        if (unauthorizedCapital.HasValue)
        {
            UnauthorizedCapital = unauthorizedCapital;
        }
    }
    public Guid DisclosureId { get; private set; }

    public long TracingNo { get; private set; }

    public string? PeriodEndToDate { get; private set; }

    public string? YearEndToDate { get; private set; }

    public string? Period { get; private set; }

    public string? Type { get; private set; }

    public int SheetCode { get; private set; }

    public int MetaTableId { get; private set; }

    public int MetaTableCode { get; private set; }

    public string? TitleFa { get; private set; }

    public string? TitleEn { get; private set; }

    public PortfolioSourceType SourceType { get; private set; }

    public PortfolioAuditStatus AuditStatus { get; private set; }

    public string? ReportSymbol { get; private set; }

    public string? ReportCompanyName { get; private set; }

    public decimal? RegisteredCapital { get; private set; }

    public decimal? UnauthorizedCapital { get; private set; }
    public DateTime ParsedAt { get; private set; }
}