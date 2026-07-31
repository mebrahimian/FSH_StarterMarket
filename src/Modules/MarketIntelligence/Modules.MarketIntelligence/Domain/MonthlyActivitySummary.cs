using FSH.Framework.Core.Domain;
namespace FSH.Modules.MarketIntelligence.Data;

public sealed class MonthlyActivitySummary :
    BaseEntity<Guid>,
    IGlobalEntity
{
    private MonthlyActivitySummary()
    {
    }

    public MonthlyActivitySummary(
        string symbol,
        string periodEndDate,
        string? yearEndDate,
        byte? rt,
        decimal? periodAmount,
        decimal? yearToDateAmount,
        decimal? previousYearToDateAmount,
        string? periodFormula,
        string? periodAddress,
        int periodRowSequence,
        string? yearToDateFormula,
        string? yearToDateAddress,
        int yearToDateRowSequence,
        Guid disclosureId,
        DateTime? publishDateTime,
        long tracingNo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(periodEndDate);

        Symbol = symbol;
        PeriodEndDate = periodEndDate;
        YearEndDate = yearEndDate;
        Rt = rt;

        PeriodAmount = periodAmount;
        PeriodFormula = periodFormula;
        PeriodAddress = periodAddress;
        PeriodRowSequence = periodRowSequence;

        YearToDateAmount = yearToDateAmount;
        PreviousYearToDateAmount = previousYearToDateAmount;
        YearToDateFormula = yearToDateFormula;
        YearToDateAddress = yearToDateAddress;
        YearToDateRowSequence = yearToDateRowSequence;

        DisclosureId = disclosureId;
        PublishDateTime = publishDateTime;
        TracingNo = tracingNo;
        ParsedAt = DateTime.UtcNow;
    }

    public string Symbol { get; private set; } = string.Empty;

    public string PeriodEndDate { get; private set; } = string.Empty;

    public string? YearEndDate { get; private set; }

    public byte? Rt { get; private set; }

    public decimal? PeriodAmount { get; private set; }

    public string? PeriodFormula { get; private set; }

    public string? PeriodAddress { get; private set; }

    public int PeriodRowSequence { get; private set; }

    public decimal? YearToDateAmount { get; private set; }
    public decimal? PreviousYearToDateAmount {  get; private set; }
    public string? YearToDateFormula { get; private set; }

    public string? YearToDateAddress { get; private set; }

    public int YearToDateRowSequence { get; private set; }

    public Guid? DisclosureId { get; private set; }

    public long? TracingNo { get; private set; }

    public DateTime? PublishDateTime { get; private set; }

    public DateTime? ParsedAt { get; private set; }

    public void Update(
        string? yearEndDate,
        byte? rt,
        decimal? periodAmount,
        decimal? yearToDateAmount,
        decimal? previousYearToDateAmount,
        string? periodFormula,
        string? periodAddress,
        int periodRowSequence,
        string? yearToDateFormula,
        string? yearToDateAddress,
        int yearToDateRowSequence,
        DateTime? publishDateTime,
        Guid disclosureId,
        long tracingNo)
    {
        YearEndDate = yearEndDate;
        Rt = rt;

        PeriodAmount = periodAmount;
        PeriodFormula = periodFormula;
        PeriodAddress = periodAddress;
        PeriodRowSequence = periodRowSequence;

        YearToDateAmount = yearToDateAmount;
        PreviousYearToDateAmount = previousYearToDateAmount;    
        YearToDateFormula = yearToDateFormula;
        YearToDateAddress = yearToDateAddress;
        YearToDateRowSequence = yearToDateRowSequence;

        PublishDateTime = publishDateTime;
        DisclosureId = disclosureId;
        TracingNo = tracingNo;
        ParsedAt = DateTime.UtcNow;
    }
}