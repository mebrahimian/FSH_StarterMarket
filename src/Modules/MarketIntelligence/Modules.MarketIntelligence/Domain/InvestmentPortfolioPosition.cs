using FSH.Framework.Core.Domain;
using FSH.Modules.MarketIntelligence.Domain.Enums;

namespace FSH.Modules.MarketIntelligence.Domain;

public sealed class InvestmentPortfolioPosition :
    BaseEntity<Guid>,
    IGlobalEntity
{
    private InvestmentPortfolioPosition()
    {
    }

    public InvestmentPortfolioPosition(
        int parentCompanyId,
        int? childCompanyId,
        string rawCompanyName,
        string fSortName,
        string periodEndDate,
        PortfolioSourceType sourceType,
        PortfolioAuditStatus auditStatus,
        bool isListed,
        int rowSequence,
        decimal? capital,
        decimal? nominalValue,
        decimal? beginningQuantity,
        decimal? beginningCost,
        decimal? beginningMarketValue,
        decimal? changeQuantity,
        decimal? changeCost,
        decimal? changeMarketValue,
        decimal? ownershipPercent,
        decimal? endingQuantity,
        decimal? endingCost,
        decimal? endingMarketValue,
        decimal? endingCostPerShare,
        decimal? endingMarketPrice,
        decimal? increaseDecrease,
        string? notes,
        Guid disclosureId,
        long tracingNo,
        DateTime? publishDateTime)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(parentCompanyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(fSortName);
        ArgumentException.ThrowIfNullOrWhiteSpace(rawCompanyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(periodEndDate);

        ParentCompanyId = parentCompanyId;
        ChildCompanyId = childCompanyId;
        RawCompanyName = rawCompanyName;
        FSortName = fSortName;
        PeriodEndDate = periodEndDate;
        SourceType = sourceType;
        AuditStatus = auditStatus;
        IsListed = isListed;
        RowSequence = rowSequence;

        Capital = capital;
        NominalValue = nominalValue;

        BeginningQuantity = beginningQuantity;
        BeginningCost = beginningCost;
        BeginningMarketValue = beginningMarketValue;

        ChangeQuantity = changeQuantity;
        ChangeCost = changeCost;
        ChangeMarketValue = changeMarketValue;

        OwnershipPercent = ownershipPercent;

        EndingQuantity = endingQuantity;
        EndingCost = endingCost;
        EndingMarketValue = endingMarketValue;
        EndingCostPerShare = endingCostPerShare;
        EndingMarketPrice = endingMarketPrice;

        IncreaseDecrease = increaseDecrease;
        Notes = notes;

        DisclosureId = disclosureId;
        TracingNo = tracingNo;
        PublishDateTime = publishDateTime;
        ParsedAt = DateTime.UtcNow;
    }

    public int ParentCompanyId { get; private set; }

    public int? ChildCompanyId { get; private set; }

    public string RawCompanyName { get; private set; } = string.Empty;
    public string FSortName { get; private set; } = string.Empty;

    public string PeriodEndDate { get; private set; } = string.Empty;
    public PortfolioSourceType SourceType { get; private set; }

    public PortfolioAuditStatus AuditStatus { get; private set; }
    public bool IsListed { get; private set; }

    public int RowSequence { get; private set; }

    public decimal? Capital { get; private set; }

    public decimal? NominalValue { get; private set; }

    public decimal? BeginningQuantity { get; private set; }

    public decimal? BeginningCost { get; private set; }

    public decimal? BeginningMarketValue { get; private set; }

    public decimal? ChangeQuantity { get; private set; }

    public decimal? ChangeCost { get; private set; }

    public decimal? ChangeMarketValue { get; private set; }

    public decimal? OwnershipPercent { get; private set; }

    public decimal? EndingQuantity { get; private set; }

    public decimal? EndingCost { get; private set; }

    public decimal? EndingMarketValue { get; private set; }

    public decimal? EndingCostPerShare { get; private set; }

    public decimal? EndingMarketPrice { get; private set; }

    public decimal? IncreaseDecrease { get; private set; }

    public string? Notes { get; private set; }

    public Guid DisclosureId { get; private set; }

    public long TracingNo { get; private set; }

    public DateTime? PublishDateTime { get; private set; }

    public DateTime ParsedAt { get; private set; }
}