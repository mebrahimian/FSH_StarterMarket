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
        bool   isListed)
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
    }

    public int CompanyId { get; private set; }

    public string? Symbol { get; private set; }
    public string? FSortSymbol { get; private set; }

    public string AliasName { get; private set; } = string.Empty;

    public string FSortName { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }
    public bool IsListed { get; private init; }
}