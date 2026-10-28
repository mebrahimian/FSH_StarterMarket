using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Domain.Enums;
using FSH.Modules.MarketIntelligence.Services.MarketData;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MarketIntelligence.Services.Portfolio;

public sealed class PortfolioValuationReportCalculatorService(
    MarketIntelligenceDbContext dbContext,
    DailyPriceMarketPriceProvider priceProvider)
{
    public async Task<PortfolioValuationAggregate> CalculateAsync(
        int companyId,
        string periodEndDate,
        Guid disclosureId,
        DateOnly valuationAsOfDate,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(companyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(periodEndDate);

        if (disclosureId == Guid.Empty)
        {
            throw new ArgumentException(
                "DisclosureId is required.",
                nameof(disclosureId));
        }

        // Only monthly portfolio positions.
        var positions = await dbContext.InvestmentPortfolioPositions
            .AsNoTracking()
            .Where(p =>
                p.ParentCompanyId == companyId &&
                p.PeriodEndDate == periodEndDate &&
                p.DisclosureId == disclosureId &&
                p.SourceType == PortfolioSourceType.MonthlyActivity)
            .ToListAsync(cancellationToken);

        if (positions.Count == 0)
        {
            throw new InvalidOperationException(
                $"No monthly portfolio positions found for {companyId}, {periodEndDate}.");
        }

        // Registered capital from the same disclosure.
        decimal? registeredCapital =
            await dbContext.InvestmentPortfolioReportMetadata
                .AsNoTracking()
                .Where(m =>
                    m.DisclosureId == disclosureId &&
                    m.SourceType == PortfolioSourceType.MonthlyActivity &&
                    m.RegisteredCapital.HasValue)
                .Select(m => m.RegisteredCapital)
                .FirstOrDefaultAsync(cancellationToken);

        // Listed holdings plus the parent investment company.
        int[] companyIds = positions
            .Where(p =>
                p.IsListed &&
                p.ChildCompanyId.HasValue)
            .Select(p => p.ChildCompanyId!.Value)
            .Append(companyId)
            .Distinct()
            .ToArray();

        // Historical prices: never later than valuationAsOfDate.
        var prices = await priceProvider.GetPricesAsOfAsync(
            companyIds,
            valuationAsOfDate,
            cancellationToken);

        return PortfolioValuationAggregator.Calculate(
            companyId,
            positions,
            registeredCapital,
            prices);
    }
}