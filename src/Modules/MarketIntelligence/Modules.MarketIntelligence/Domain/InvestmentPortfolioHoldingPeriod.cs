using FSH.Framework.Core.Domain;

namespace FSH.Modules.MarketIntelligence.Domain;

public sealed class InvestmentPortfolioHoldingPeriod :
    BaseEntity<int>,
    IGlobalEntity
{
    private InvestmentPortfolioHoldingPeriod()
    {
    }

    public InvestmentPortfolioHoldingPeriod(
        string parentSymbol,
        int holdingAssetId,
        string changeDate,
        decimal currentQuantity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parentSymbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(changeDate);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(holdingAssetId);
        ArgumentOutOfRangeException.ThrowIfNegative(currentQuantity);

        ParentSymbol = parentSymbol.Trim();
        HoldingAssetId = holdingAssetId;
        ChangeDate = changeDate;
        CurrentQuantity = currentQuantity;
    }

    public string ParentSymbol { get; private set; } = string.Empty;

    public int HoldingAssetId { get; private set; }

    public string ChangeDate { get; private set; } = string.Empty;

    public decimal CurrentQuantity { get; private set; }

    public void CorrectQuantity(decimal currentQuantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(currentQuantity);
        CurrentQuantity = currentQuantity;
    }
}