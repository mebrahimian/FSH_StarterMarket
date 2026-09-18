using FSH.Framework.Core.Domain;

namespace FSH.Modules.MarketIntelligence.Domain;

public sealed class PortfolioHoldingAsset :
    BaseEntity<int>,
    IGlobalEntity
{
    private PortfolioHoldingAsset()
    {
    }

    public PortfolioHoldingAsset(
        string name,
        string fSortName,
        int companyId,
        bool isListed,
        string? symbol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(fSortName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(companyId);

        Name = name.Trim();
        FSortName = fSortName;

        if (isListed)
        {
            ListedCompanyId = companyId;
            Symbol = symbol;
        }
        else
        {
            UnlistedCompanyId = companyId;
        }
    }

    public string Name { get; private set; } = string.Empty;

    public string FSortName { get; private set; } = string.Empty;

    public int? UnlistedCompanyId { get; private set; }

    public int? ListedCompanyId { get; private set; }

    public string? Symbol { get; private set; }

    public void LinkUnlistedCompany(int companyId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            companyId);

        UnlistedCompanyId = companyId;
    }

    public void LinkListedCompany(
        int companyId,
        string symbol)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            companyId);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            symbol);

        ListedCompanyId = companyId;
        Symbol = symbol.Trim();
    }
}