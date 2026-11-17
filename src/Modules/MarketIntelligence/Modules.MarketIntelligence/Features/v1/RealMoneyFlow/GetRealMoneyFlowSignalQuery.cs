
using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Services.Tsetmc;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MarketIntelligence.Features.v1.RealMoneyFlow;

public sealed record GetRealMoneyFlowSignalQuery(
    int CompanyId,
    DateOnly? AsOfDate = null)
    : IQuery<RealMoneyFlowSignalResponse?>;

public sealed record RealMoneyFlowSignalResponse(
    int CompanyId,
    string Symbol,
    DateOnly StartDate,
    DateOnly EndDate,
    long NetFlow,
    decimal ZScore,
    decimal FlowMultiple,
    decimal TradeValueMultiple,
    bool IsUnusual,
    long StartPrice,
    long EndPrice,
    decimal RawPriceChangePercent,
    decimal? AdjustedPriceChangePercent,
    decimal AdjustmentFactor,
    PriceAdjustmentStatus AdjustmentStatus);

public sealed class GetRealMoneyFlowSignalQueryHandler(
    MarketIntelligenceDbContext dbContext,
    CapitalIncreasePriceAdjustmentService adjustmentService)
    : IQueryHandler<
        GetRealMoneyFlowSignalQuery,
        RealMoneyFlowSignalResponse?>
{
    public async ValueTask<RealMoneyFlowSignalResponse?> Handle(
        GetRealMoneyFlowSignalQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            query.CompanyId);

        var company = await dbContext.CompanyMaster
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.CompanyId == query.CompanyId,
                cancellationToken);

        if (company is null ||
            !company.IsListed ||
            string.IsNullOrWhiteSpace(company.FSortSymbol))
        {
            return null;
        }

        // Resolve the company's TSETMC instrument.
        var instruments = await dbContext.TsetmcInstruments
            .AsNoTracking()
            .Where(x =>
                x.NormalizedSymbol == company.FSortSymbol)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (instruments.Count == 0)
            return null;

        if (instruments.Count > 1)
        {
            throw new InvalidOperationException(
                $"Ambiguous TSETMC instrument for CompanyId={query.CompanyId}.");
        }

        int instrumentId = instruments[0].Id;

        var pricesQuery = dbContext.DailyPrices
            .AsNoTracking()
            .Where(x =>
                x.InstrumentId == instrumentId &&
                x.RealMoneyFlow.HasValue &&
                x.Value > 0 &&
                x.ClosingPrice > 0);

        if (query.AsOfDate is { } asOfDate)
        {
            pricesQuery = pricesQuery.Where(
                x => x.TradeDate <= asOfDate);
        }

        var prices = await pricesQuery
            .OrderByDescending(x => x.TradeDate)
            .Take(21)
            .ToListAsync(cancellationToken);

        var signal =
            RealMoneyFlowSignalCalculator.Calculate(prices);

        if (signal is null)
            return null;

        var current = prices[0];
        var first = prices[^1];

        var adjustment = await adjustmentService.CalculateAsync(
            instrumentId,
            first.TradeDate,
            current.TradeDate,
            first.ClosingPrice,
            current.ClosingPrice,
            cancellationToken);

        return new RealMoneyFlowSignalResponse(
            query.CompanyId,
            company.Symbol ?? instruments[0].Symbol,
            first.TradeDate,
            current.TradeDate,
            signal.NetFlow,
            signal.ZScore,
            signal.FlowMultiple,
            signal.TradeValueMultiple,
            signal.IsUnusual,
            first.ClosingPrice,
            current.ClosingPrice,
            adjustment.RawPriceChangePercent,
            adjustment.AdjustedPriceChangePercent,
            adjustment.AdjustmentFactor,
            adjustment.Status);
    }
}
