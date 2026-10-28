
using FSH.Framework.Core.Domain;

namespace Modules.MarketIntelligence.Domain;

public sealed class PortfolioValuationSnapshot
    : BaseEntity<int>, IGlobalEntity
{
    // Identity
    public int CompanyId { get; set; }

    public string PeriodEndDate { get; set; } = default!;

    // =========================
    // Current monthly portfolio
    // =========================

    public Guid CurrentDisclosureId { get; set; }

    public decimal? CurrentRegisteredCapital { get; set; }

    public decimal? CurrentListedValue { get; set; }

    public decimal? CurrentUnlistedValue { get; set; }

    public decimal? CurrentPortfolioValue { get; set; }

    public decimal? CurrentValuePerShare { get; set; }

    public decimal? CurrentSharePrice { get; set; }

    public string? CurrentPriceTradeDate { get; set; }

    public decimal? CurrentPriceToPortfolioValuePercent { get; set; }

    public int CurrentListedPositionCount { get; set; }

    public int CurrentPricedPositionCount { get; set; }

    public DateOnly? CurrentValuationAsOfDate { get; set; }

    public DateTimeOffset CurrentCalculatedAt { get; set; }

    // =========================
    // Previous monthly portfolio
    // =========================

    public Guid? PreviousDisclosureId { get; set; }

    public string? PreviousPeriodEndDate { get; set; }

    public decimal? PreviousRegisteredCapital { get; set; }

    public decimal? PreviousListedValue { get; set; }

    public decimal? PreviousUnlistedValue { get; set; }

    public decimal? PreviousPortfolioValue { get; set; }

    public decimal? PreviousValuePerShare { get; set; }

    public decimal? PreviousSharePrice { get; set; }

    public string? PreviousPriceTradeDate { get; set; }

    public decimal? PreviousPriceToPortfolioValuePercent { get; set; }

    public int? PreviousListedPositionCount { get; set; }

    public int? PreviousPricedPositionCount { get; set; }

    public DateOnly? PreviousValuationAsOfDate { get; set; }

    public DateTimeOffset? PreviousCalculatedAt { get; set; }

    // =========================
    // Metadata
    // =========================

    public DateTimeOffset? FrozenAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}