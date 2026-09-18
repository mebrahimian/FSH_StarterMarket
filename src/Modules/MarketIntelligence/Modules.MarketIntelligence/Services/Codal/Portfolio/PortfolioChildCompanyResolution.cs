namespace FSH.Modules.MarketIntelligence.Services.Codal.Portfolio;

public sealed record PortfolioChildCompanyResolution(
    int? CompanyId,
    int? HoldingAssetId,
    bool IsListed,
    bool IsExcluded,
    string FSortName)
{
    public bool IsResolved => CompanyId.HasValue;
}