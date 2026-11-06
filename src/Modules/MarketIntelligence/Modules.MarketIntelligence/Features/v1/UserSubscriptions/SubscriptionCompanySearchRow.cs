namespace FSH.Modules.MarketIntelligence.Features.v1.UserSubscriptions;

public sealed class SubscriptionCompanySearchRow
{
    public int CompanyId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string NormalizedSymbol { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
}