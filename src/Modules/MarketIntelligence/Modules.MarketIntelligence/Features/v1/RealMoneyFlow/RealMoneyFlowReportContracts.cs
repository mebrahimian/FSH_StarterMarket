
using FSH.Modules.MarketIntelligence.Services.Tsetmc;

namespace FSH.Modules.MarketIntelligence.Features.v1.RealMoneyFlow;

public sealed record RealMoneyFlowReportRequest(
    DateOnly? TradeDate = null,
    int LookbackDays = 20,
    decimal MinZScore = 1.5m,
    decimal MinFlowMultiple = 2m,
    decimal MinTradeValueMultiple = 1.5m,
    int? CompanyId = null,
    int PageNumber = 1,
    int PageSize = 20,
    RealMoneyFlowDirection Direction = RealMoneyFlowDirection.Inflow);

public sealed record RealMoneyFlowReportItem(
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
