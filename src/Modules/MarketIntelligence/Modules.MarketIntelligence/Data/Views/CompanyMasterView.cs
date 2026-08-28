namespace FSH.Modules.MarketIntelligence.Data.Views;

public sealed class CompanyMasterView
{
    public int CompanyId { get; init; }

    public string? Symbol { get; init; }

    public string? FSortSymbol { get; init; }

    public string CompanyName { get; init; } = string.Empty;

    public string FSortName { get; init; } = string.Empty;

    public bool IsListed { get; init; }
}