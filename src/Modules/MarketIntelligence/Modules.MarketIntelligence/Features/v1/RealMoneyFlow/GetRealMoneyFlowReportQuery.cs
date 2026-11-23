
using System.Globalization;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Features.v1.UserSubscriptions;
using FSH.Modules.MarketIntelligence.Services.Tsetmc;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MarketIntelligence.Features.v1.RealMoneyFlow;

public sealed record GetRealMoneyFlowReportQuery(
    RealMoneyFlowReportRequest Request)
    : IQuery<PagedResponse<RealMoneyFlowReportItem>>;

public sealed class GetRealMoneyFlowReportQueryHandler(
    MarketIntelligenceDbContext dbContext,
    IMediator mediator,
    CapitalIncreasePriceAdjustmentService adjustmentService)
    : IQueryHandler<
        GetRealMoneyFlowReportQuery,
        PagedResponse<RealMoneyFlowReportItem>>
{
    public async ValueTask<PagedResponse<RealMoneyFlowReportItem>> Handle(
        GetRealMoneyFlowReportQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(query.Request);

        var request = query.Request;

        if (request.LookbackDays is < 2 or > 252 ||
            request.MinZScore < 0 ||
            request.MinFlowMultiple < 0 ||
            request.MinTradeValueMultiple < 0 ||
            request.PageNumber < 1 ||
            request.PageSize is < 1 or > 100 ||
            !Enum.IsDefined(request.Direction))
        {
            throw new CustomException(
                "Invalid real money flow parameters.",
                Array.Empty<string>(),
                System.Net.HttpStatusCode.BadRequest);
        }

        var selections = await mediator.Send(
            new GetMySubscriptionSelectionsQuery(),
            cancellationToken);

        int allowedChanges = selections.PlanKey switch
        {
            "free" => 0,
            "economic" => 1,
            "professional" => 2,
            "premium" => 4,
            _ => 0
        };

        int changedParameters =
            (request.LookbackDays != 20 ? 1 : 0) +
            (request.MinZScore != 1.5m ? 1 : 0) +
            (request.MinFlowMultiple != 2m ? 1 : 0) +
            (request.MinTradeValueMultiple != 1.5m ? 1 : 0);

        if (changedParameters > allowedChanges)
        {
            throw new ForbiddenException(
                "Your subscription does not allow changing this many parameters.");
        }

        bool allCompanies =
            selections.PlanKey == "premium" &&
            selections.MaxSelectedSymbols == -1;

        int selectionLimit = Math.Max(
            0, selections.MaxSelectedSymbols);

        int[] selectedIds = selections.SelectedCompanyIds
            .Take(selectionLimit)
            .Select(x => x.CompanyId)
            .Distinct()
            .ToArray();

        if (request.CompanyId is { } requestedCompanyId &&
            !allCompanies &&
            !selectedIds.Contains(requestedCompanyId))
        {
            throw new ForbiddenException(
                "This company is not among your selected symbols.");
        }

        var companyQuery = dbContext.CompanyMaster
            .AsNoTracking()
            .Where(x =>
                x.IsListed &&
                x.FSortSymbol != null);

        if (!allCompanies)
        {
            companyQuery = companyQuery.Where(
                x => selectedIds.Contains(x.CompanyId));
        }

        if (request.CompanyId is { } companyId)
        {
            companyQuery = companyQuery.Where(
                x => x.CompanyId == companyId);
        }

        var companies = await companyQuery
            .Select(x => new
            {
                x.CompanyId,
                x.Symbol,
                x.FSortSymbol
            })
            .ToListAsync(cancellationToken);

        if (companies.Count == 0)
            return Empty(request);

        string[] symbols = companies
            .Select(x => x.FSortSymbol!)
            .Distinct()
            .ToArray();

        var instruments = await dbContext.TsetmcInstruments
            .AsNoTracking()
            .Where(x => symbols.Contains(x.NormalizedSymbol))
            .ToListAsync(cancellationToken);

        var instrumentBySymbol = instruments
            .GroupBy(x => x.NormalizedSymbol)
            .ToDictionary(
                x => x.Key,
                x => x
                    .OrderByDescending(i => i.LastSeenAt)
                    .ThenByDescending(i => i.Id)
                    .First());

        var mappings = companies
            .Where(x =>
                instrumentBySymbol.ContainsKey(x.FSortSymbol!))
            .Select(x => new
            {
                x.CompanyId,
                Symbol = x.Symbol ??
                    instrumentBySymbol[x.FSortSymbol!].Symbol,
                InstrumentId =
                    instrumentBySymbol[x.FSortSymbol!].Id
            })
            .ToList();

        if (mappings.Count == 0)
            return Empty(request);

        int[] instrumentIds = mappings
            .Select(x => x.InstrumentId)
            .Distinct()
            .ToArray();

        DateOnly? asOfDate = request.TradeDate;

        if (selections.PlanKey == "free")
        {
            DateTime tehranNow = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran"));

            var calendar = new PersianCalendar();

            DateTime firstDayOfCurrentMonth = calendar.ToDateTime(
                calendar.GetYear(tehranNow),
                calendar.GetMonth(tehranNow),
                1, 0, 0, 0, 0);

            DateOnly freeCutoff = DateOnly.FromDateTime(
                firstDayOfCurrentMonth.AddDays(-1));

            if (!asOfDate.HasValue ||
                asOfDate.Value > freeCutoff)
            {
                asOfDate = freeCutoff;
            }
        }

        var dateQuery = dbContext.DailyPrices
            .AsNoTracking()
            .Where(x =>
                instrumentIds.Contains(x.InstrumentId) &&
                x.RealMoneyFlow.HasValue &&
                x.Value > 0 &&
                x.ClosingPrice > 0);

        if (asOfDate.HasValue)
        {
            dateQuery = dateQuery.Where(
                x => x.TradeDate <= asOfDate.Value);
        }

        DateOnly? tradeDate = await dateQuery
            .MaxAsync(
                x => (DateOnly?)x.TradeDate,
                cancellationToken);

        if (!tradeDate.HasValue)
            return Empty(request);

        // Bounded calendar window for the trading-day lookback.
        DateOnly earliestDate = tradeDate.Value.AddDays(
            -(request.LookbackDays * 2 + 30));

        var prices = await dbContext.DailyPrices
            .AsNoTracking()
            .Where(x =>
                instrumentIds.Contains(x.InstrumentId) &&
                x.TradeDate >= earliestDate &&
                x.TradeDate <= tradeDate.Value &&
                x.RealMoneyFlow.HasValue &&
                x.Value > 0 &&
                x.ClosingPrice > 0)
            .ToListAsync(cancellationToken);

        int requiredDays = request.LookbackDays + 1;

        var priceByInstrument = prices
            .GroupBy(x => x.InstrumentId)
            .ToDictionary(
                x => x.Key,
                x => x.OrderByDescending(p => p.TradeDate)
                    .Take(requiredDays)
                    .ToList());

        var parameters = new RealMoneyFlowSignalParameters(
            request.LookbackDays,
            request.MinZScore,
            request.MinFlowMultiple,
            request.MinTradeValueMultiple);

        var candidates = new List<Candidate>();

        foreach (var mapping in mappings)
        {
            if (!priceByInstrument.TryGetValue(
                    mapping.InstrumentId,
                    out var history) ||
                history.Count != requiredDays ||
                history[0].TradeDate != tradeDate.Value)
            {
                continue;
            }

            var signal = RealMoneyFlowSignalCalculator.Calculate(
                history,
                parameters,
                request.Direction);

            if (signal?.IsUnusual != true)
                continue;

            var current = history[0];
            var first = history[^1];

            candidates.Add(new Candidate(
                mapping.CompanyId,
                mapping.Symbol,
                mapping.InstrumentId,
                first.TradeDate,
                current.TradeDate,
                first.ClosingPrice,
                current.ClosingPrice,
                signal));
        }

        var pageItems = candidates
            .OrderByDescending(x => x.Signal.ZScore)
            .ThenByDescending(x => x.Signal.NetFlow)
            .ThenBy(x => x.CompanyId)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var items = new List<RealMoneyFlowReportItem>();

        foreach (var item in pageItems)
        {
            var adjustment = await adjustmentService.CalculateAsync(
                item.InstrumentId,
                item.StartDate,
                item.EndDate,
                item.StartPrice,
                item.EndPrice,
                cancellationToken);

            items.Add(new RealMoneyFlowReportItem(
                item.CompanyId,
                item.Symbol,
                item.StartDate,
                item.EndDate,
                item.Signal.NetFlow,
                item.Signal.ZScore,
                item.Signal.FlowMultiple,
                item.Signal.TradeValueMultiple,
                item.Signal.IsUnusual,
                item.StartPrice,
                item.EndPrice,
                adjustment.RawPriceChangePercent,
                adjustment.AdjustedPriceChangePercent,
                adjustment.AdjustmentFactor,
                adjustment.Status));
        }

        return new PagedResponse<RealMoneyFlowReportItem>
        {
            Items = items,
            TotalCount = candidates.Count,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling(
                candidates.Count / (double)request.PageSize)
        };
    }

    private static PagedResponse<RealMoneyFlowReportItem> Empty(
        RealMoneyFlowReportRequest request)
    {
        return new PagedResponse<RealMoneyFlowReportItem>
        {
            Items = new List<RealMoneyFlowReportItem>(),
            TotalCount = 0,
            TotalPages = 0,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    private sealed record Candidate(
        int CompanyId,
        string Symbol,
        int InstrumentId,
        DateOnly StartDate,
        DateOnly EndDate,
        long StartPrice,
        long EndPrice,
        RealMoneyFlowSignal Signal);
}
