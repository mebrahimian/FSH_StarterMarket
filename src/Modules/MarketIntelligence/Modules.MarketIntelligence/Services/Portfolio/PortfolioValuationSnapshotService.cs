using FSH.Modules.MarketIntelligence.Data;
using Microsoft.EntityFrameworkCore;
using Modules.MarketIntelligence.Domain;

namespace FSH.Modules.MarketIntelligence.Services.Portfolio;

public sealed class PortfolioValuationSnapshotService(
    MarketIntelligenceDbContext dbContext)
{
    public async Task UpsertAsync(
        int companyId,
        string periodEndDate,
        Guid disclosureId,
        PortfolioValuationAggregate valuation,
        DateOnly valuationAsOfDate,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(companyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(periodEndDate);
        ArgumentNullException.ThrowIfNull(valuation);

        if (disclosureId == Guid.Empty)
        {
            throw new ArgumentException(
                "DisclosureId is required.",
                nameof(disclosureId));
        }

        var now = DateTimeOffset.UtcNow;

        var snapshot = await dbContext.PortfolioValuationSnapshots
            .FirstOrDefaultAsync(
                x => x.CompanyId == companyId &&
                     x.PeriodEndDate == periodEndDate,
                cancellationToken);

        if (snapshot is not null)
        {
            // Historical snapshots must not change silently.
            if (snapshot.FrozenAt.HasValue)
            {
                throw new InvalidOperationException(
                    "Frozen snapshots require an explicit historical rebuild.");
            }

            SetCurrent(
                snapshot,
                disclosureId,
                valuation,
                valuationAsOfDate,
                now);

            snapshot.UpdatedAt = now;
        }
        else
        {
            var previous = await dbContext.PortfolioValuationSnapshots
                .Where(x => x.CompanyId == companyId)
                .OrderByDescending(x => x.PeriodEndDate)
                .FirstOrDefaultAsync(cancellationToken);

            // Backfill must insert reporting periods chronologically.
            if (previous is not null &&
                string.CompareOrdinal(
                    previous.PeriodEndDate,
                    periodEndDate) >= 0)
            {
                throw new InvalidOperationException(
                    "New reporting periods must be inserted in chronological order.");
            }

            snapshot = new PortfolioValuationSnapshot
            {
                CompanyId = companyId,
                PeriodEndDate = periodEndDate,
                CreatedAt = now,
                UpdatedAt = now
            };

            if (previous is not null)
            {
                CopyToPrevious(snapshot, previous);

                previous.FrozenAt ??= now;
                previous.UpdatedAt = now;
            }

            SetCurrent(
                snapshot,
                disclosureId,
                valuation,
                valuationAsOfDate,
                now);

            dbContext.PortfolioValuationSnapshots.Add(snapshot);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void SetCurrent(
        PortfolioValuationSnapshot snapshot,
        Guid disclosureId,
        PortfolioValuationAggregate valuation,
        DateOnly valuationAsOfDate,
        DateTimeOffset calculatedAt)
    {
        snapshot.CurrentDisclosureId = disclosureId;

        snapshot.CurrentRegisteredCapital =
            valuation.RegisteredCapital;

        snapshot.CurrentListedValue =
            valuation.ListedValue;

        snapshot.CurrentUnlistedValue =
            valuation.UnlistedValue;

        snapshot.CurrentPortfolioValue =
            valuation.PortfolioValue;

        snapshot.CurrentValuePerShare =
            valuation.ValuePerShare;

        snapshot.CurrentSharePrice =
            valuation.SharePrice;

        snapshot.CurrentPriceTradeDate =
            valuation.PriceTradeDate;

        snapshot.CurrentPriceToPortfolioValuePercent =
            valuation.PriceToPortfolioValuePercent;

        snapshot.CurrentListedPositionCount =
            valuation.ListedPositionCount;

        snapshot.CurrentPricedPositionCount =
            valuation.PricedPositionCount;

        snapshot.CurrentValuationAsOfDate =
            valuationAsOfDate;

        snapshot.CurrentCalculatedAt =
            calculatedAt;
    }

    private static void CopyToPrevious(
        PortfolioValuationSnapshot target,
        PortfolioValuationSnapshot source)
    {
        target.PreviousDisclosureId =
            source.CurrentDisclosureId;

        target.PreviousPeriodEndDate =
            source.PeriodEndDate;

        target.PreviousRegisteredCapital =
            source.CurrentRegisteredCapital;

        target.PreviousListedValue =
            source.CurrentListedValue;

        target.PreviousUnlistedValue =
            source.CurrentUnlistedValue;

        target.PreviousPortfolioValue =
            source.CurrentPortfolioValue;

        target.PreviousValuePerShare =
            source.CurrentValuePerShare;

        target.PreviousSharePrice =
            source.CurrentSharePrice;

        target.PreviousPriceTradeDate =
            source.CurrentPriceTradeDate;

        target.PreviousPriceToPortfolioValuePercent =
            source.CurrentPriceToPortfolioValuePercent;

        target.PreviousListedPositionCount =
            source.CurrentListedPositionCount;

        target.PreviousPricedPositionCount =
            source.CurrentPricedPositionCount;

        target.PreviousValuationAsOfDate =
            source.CurrentValuationAsOfDate;

        target.PreviousCalculatedAt =
            source.CurrentCalculatedAt;
    }
}