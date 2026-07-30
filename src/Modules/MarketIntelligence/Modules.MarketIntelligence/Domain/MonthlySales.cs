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
        string? yearEndDate,
        decimal? monthlySalesAmount,
        decimal? yearToDateSalesAmount,
        string? monthlySalesFormula,
        string? monthlySalesAddress,
        int monthlySalesRowSequence,
        string? yearToDateSalesFormula,
        string? yearToDateSalesAddress,
        int yearToDateSalesRowSequence,
        Guid disclosureId,
        DateTime? publishDateTime,
        long tracingNo
        )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(periodEndDate);

        Symbol = symbol;
        PeriodEndDate = periodEndDate;
        YearEndDate = yearEndDate;

        MonthlySalesAmount = monthlySalesAmount;
        MonthlySalesFormula = monthlySalesFormula;
        MonthlySalesAddress = monthlySalesAddress;
        MonthlySalesRowSequence = monthlySalesRowSequence;

        YearToDateSalesAmount = yearToDateSalesAmount;
        YearToDateSalesFormula = yearToDateSalesFormula;
        YearToDateSalesAddress = yearToDateSalesAddress;
        YearToDateSalesRowSequence = yearToDateSalesRowSequence;

        DisclosureId = disclosureId;
        PublishDateTime = publishDateTime;  
        TracingNo = tracingNo;
        ParsedAt = DateTime.UtcNow;
    }

    public string Symbol { get; private set; } = string.Empty;

    public string PeriodEndDate { get; private set; } = string.Empty;

    public string? YearEndDate { get; private set; }

    public decimal? MonthlySalesAmount { get; private set; }

    public string? MonthlySalesFormula { get; private set; }

    public string? MonthlySalesAddress { get; private set; }

    public int MonthlySalesRowSequence { get; private set; }

    public decimal? YearToDateSalesAmount { get; private set; }

    public string? YearToDateSalesFormula { get; private set; }

    public string? YearToDateSalesAddress { get; private set; }

    public int YearToDateSalesRowSequence { get; private set; }

    public Guid? DisclosureId { get; private set; }

    public long? TracingNo { get; private set; }

    public DateTime? PublishDateTime { get; private set; }
    public DateTime? ParsedAt { get; private set; }

    public void Update(
        string? yearEndDate,
        decimal? monthlySalesAmount,
        decimal? yearToDateSalesAmount,
        string? monthlySalesFormula,
        string? monthlySalesAddress,
        int monthlySalesRowSequence,
        string? yearToDateSalesFormula,
        string? yearToDateSalesAddress,
        int yearToDateSalesRowSequence,
        DateTime? publishDateTime,
        Guid disclosureId,
        long tracingNo)
    {
        YearEndDate = yearEndDate;

        MonthlySalesAmount = monthlySalesAmount;
        MonthlySalesFormula = monthlySalesFormula;
        MonthlySalesAddress = monthlySalesAddress;
        MonthlySalesRowSequence = monthlySalesRowSequence;

        YearToDateSalesAmount = yearToDateSalesAmount;
        YearToDateSalesFormula = yearToDateSalesFormula;
        YearToDateSalesAddress = yearToDateSalesAddress;
        YearToDateSalesRowSequence = yearToDateSalesRowSequence;

        PublishDateTime = publishDateTime;
        DisclosureId = disclosureId;
        TracingNo = tracingNo;
        ParsedAt = DateTime.UtcNow;
    }
}