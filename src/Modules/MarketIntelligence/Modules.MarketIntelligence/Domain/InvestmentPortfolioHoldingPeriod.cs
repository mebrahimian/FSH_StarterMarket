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
        string entryDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parentSymbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(entryDate);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(holdingAssetId);

        ParentSymbol = parentSymbol.Trim();
        HoldingAssetId = holdingAssetId;
        EntryDate = entryDate;
        IsActive = true;
    }

    public string ParentSymbol { get; private set; } = string.Empty;

    public int HoldingAssetId { get; private set; }

    public string EntryDate { get; private set; } = string.Empty;

    public string? ExitDate { get; private set; }

    public bool IsActive { get; private set; }

    public void Close(string exitDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(exitDate);

        ExitDate = exitDate;
        IsActive = false;
    }
}