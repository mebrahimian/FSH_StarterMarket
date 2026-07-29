using FSH.Framework.Core.Domain;
using FSH.Framework.Shared.Persistence;

namespace FSH.Modules.MarketIntelligence.Domain;

public sealed class MonthlySales : BaseEntity<Guid>, IGlobalEntity
{
    private MonthlySales()
    {
    }

    public MonthlySales(
        string symbol,
        string periodEndDate,
        string? yearEndDate)
    {
        Symbol = symbol;
        PeriodEndDate = periodEndDate;
        YearEndDate = yearEndDate;
    }

    public string Symbol { get; private set; } = string.Empty;

    /// <summary>
    /// پایان دوره ماهانه (Persian yyyy/MM/dd)
    /// </summary>
    public string PeriodEndDate { get; private set; } = string.Empty;

    /// <summary>
    /// پایان سال مالی
    /// </summary>
    public string? YearEndDate { get; private set; }

    /// <summary>
    /// فروش ماه جاری
    /// </summary>
    public decimal? MonthlySalesAmount { get; private set; }

    /// <summary>
    /// فروش تجمعی ابتدای سال تا پایان این ماه
    /// </summary>
    public decimal? YearToDateSalesAmount { get; private set; }

    /// <summary>
    /// آخرین Disclosure که این اطلاعات از آن استخراج شده
    /// </summary>
    public Guid? DisclosureId { get; private set; }

    /// <summary>
    /// آخرین TracingNo
    /// </summary>
    public long? TracingNo { get; private set; }

    public DateTime? ParsedAt { get; private set; }

    public void Update(
        decimal? monthlySales,
        decimal? yearToDateSales,
        Guid disclosureId,
        long tracingNo)
    {
        MonthlySalesAmount = monthlySales;
        YearToDateSalesAmount = yearToDateSales;
        DisclosureId = disclosureId;
        TracingNo = tracingNo;
        ParsedAt = DateTime.UtcNow;
    }
}