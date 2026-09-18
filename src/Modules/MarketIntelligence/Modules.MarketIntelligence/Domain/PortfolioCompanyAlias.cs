using FSH.Framework.Core.Domain;

namespace FSH.Modules.MarketIntelligence.Domain;

public sealed class PortfolioCompanyAlias :
    BaseEntity<Guid>,
    IGlobalEntity
{
    private PortfolioCompanyAlias()
    {
    }
    
    public PortfolioCompanyAlias(
        int companyId,
        string? symbol,
        string? fSortSymbol,
        string aliasName,
        string fSortName,
        bool   isListed,
        int? holdingAssetId = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(companyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(aliasName);
        ArgumentException.ThrowIfNullOrWhiteSpace(fSortName);

        CompanyId = companyId;
        Symbol = symbol;
        FSortSymbol = fSortSymbol;
        AliasName = aliasName;
        FSortName = fSortName;
        IsActive = true;
        IsListed = isListed;
        HoldingAssetId = holdingAssetId;
    }

    public int CompanyId { get; private set; }

    public string? Symbol { get; private set; }
    public string? FSortSymbol { get; private set; }

    public string AliasName { get; private set; } = string.Empty;

    public string FSortName { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }
    public bool IsListed { get; private set; }
    public int? HoldingAssetId { get; private set; }
    public void AssignHoldingAsset(int holdingAssetId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            holdingAssetId);

        if (HoldingAssetId.HasValue &&
            HoldingAssetId.Value != holdingAssetId)
        {
            throw new InvalidOperationException(
                "Portfolio company alias is already assigned " +
                "to another holding asset.");
        }

        HoldingAssetId = holdingAssetId;
    }
}