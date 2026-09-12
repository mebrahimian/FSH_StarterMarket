namespace FSH.Modules.MarketIntelligence.Domain.Insights;

public sealed class Insight
{
    public Guid Id { get; private set; }

    public string Code { get; private set; } = default!;

    public int? CompanyId { get; private set; }

    public string? Symbol { get; private set; }

    public string? PeriodEndDate { get; private set; }

    public InsightDirection Direction { get; private set; }

    public decimal ConfidenceScore { get; private set; }

    public decimal ImpactScore { get; private set; }

    public string PayloadJson { get; private set; } = "{}";

    public DateTimeOffset DetectedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    private Insight()
    {
    }

    public Insight(
        string code,
        int? companyId,
        string? symbol,
        string? periodEndDate,
        InsightDirection direction,
        decimal confidenceScore,
        decimal impactScore,
        string payloadJson,
        DateTimeOffset detectedAt)
    {
        Id = Guid.NewGuid();
        Code = code;
        CompanyId = companyId;
        Symbol = symbol;
        PeriodEndDate = periodEndDate;
        Direction = direction;
        ConfidenceScore = confidenceScore;
        ImpactScore = impactScore;
        PayloadJson = payloadJson;
        DetectedAt = detectedAt;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}