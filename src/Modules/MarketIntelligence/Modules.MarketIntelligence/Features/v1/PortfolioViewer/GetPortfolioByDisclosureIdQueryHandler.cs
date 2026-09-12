using FSH.Modules.MarketIntelligence.Contracts.v1.PortfolioViewer;
using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Services.MarketData;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Modules.MarketIntelligence.Features.v1.PortfolioViewer;

public sealed class GetPortfolioByDisclosureIdQueryHandler(
    MarketIntelligenceDbContext dbContext,
    IMarketPriceProvider marketPriceProvider,
    ILogger<GetPortfolioByDisclosureIdQueryHandler> logger) : IQueryHandler<
        GetPortfolioByDisclosureIdQuery,
        PortfolioReportDto?>
{
    public async ValueTask<PortfolioReportDto?> Handle(
        GetPortfolioByDisclosureIdQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
    "PortfolioViewer Database={Database}, DisclosureId={DisclosureId}",
    dbContext.Database.GetDbConnection().Database,
    query.DisclosureId);
        }
        var positions =
            await dbContext.InvestmentPortfolioPositions
                .AsNoTracking()
                .Where(position =>
                    position.DisclosureId == query.DisclosureId)
                .OrderBy(position =>
                    position.RowSequence)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        if (positions.Count == 0)
        {
            return null;
        }
        logger.LogInformation("Portfolio handler started");
        var firstPosition = positions[0];

        int parentCompanyId = firstPosition.ParentCompanyId;
        string periodEndDate = firstPosition.PeriodEndDate;
        var sourceType = firstPosition.SourceType;
        var auditStatus = firstPosition.AuditStatus;

        var navigationReports = await dbContext.InvestmentPortfolioPositions
               .AsNoTracking()
               .Where(position => position.ParentCompanyId == parentCompanyId)
               .Select(position => new
                 {
                    position.DisclosureId,
                    position.PeriodEndDate,
                    position.PublishDateTime,
                    position.SourceType,
                    position.AuditStatus,
                 })
               .Distinct()
               .ToListAsync(cancellationToken)
               .ConfigureAwait(false);

        var latestReport = navigationReports
            .OrderByDescending(report => report.PeriodEndDate, StringComparer.Ordinal)
            .ThenByDescending(report => report.PublishDateTime)
            .FirstOrDefault();

        bool isLatestPortfolio = latestReport is not null &&
                                 latestReport.DisclosureId == query.DisclosureId; decimal? registeredCapital =
        await dbContext.InvestmentPortfolioReportMetadata
            .Where(x =>
                   x.DisclosureId == query.DisclosureId &&
                   x.RegisteredCapital.HasValue)
            .Select(x => x.RegisteredCapital)
            .FirstOrDefaultAsync(cancellationToken);

        logger.LogInformation("Portfolio handler started");
        var orderedReports = navigationReports
              .Where(report => report.SourceType == sourceType &&
                               report.AuditStatus == auditStatus)
              .OrderBy(report => report.PeriodEndDate, StringComparer.Ordinal)
              .ToList();

        int currentIndex = orderedReports.FindIndex(report => report.DisclosureId == query.DisclosureId);

        Guid? previousDisclosureId = currentIndex > 0
                       ? orderedReports[currentIndex - 1]
                            .DisclosureId
                       : null;

        Guid? nextDisclosureId = currentIndex >= 0 &&
                      currentIndex < orderedReports.Count - 1
                        ? orderedReports[currentIndex + 1]
                             .DisclosureId
                        : null;
        var navigationTargets = navigationReports
                .GroupBy(report => new
                    {
                       report.SourceType,
                       report.AuditStatus,
                    })
                .Select(group =>
        {
            var reports =
                group
                    .OrderBy(
                        report => report.PeriodEndDate,
                        StringComparer.Ordinal)
                    .ToList();

            var target = reports.Find(report => report.PeriodEndDate == periodEndDate)
                       ?? reports.FindLast(report => 
                           string.CompareOrdinal(report.PeriodEndDate,
                           periodEndDate) <= 0)
                       ?? reports[0];

            return new PortfolioNavigationTargetDto(
                (byte)group.Key.SourceType,
                (byte)group.Key.AuditStatus,
                target.DisclosureId,
                target.PeriodEndDate);
        })
        .ToList();

        int[] childCompanyIds =
            positions
                .Where(position =>
                    position.ChildCompanyId.HasValue)
                .Select(position =>
                    position.ChildCompanyId!.Value)
                .Distinct()
                .ToArray();

        int[] listedChildCompanyIds = positions
                .Where(position => position.IsListed &&
                                   position.ChildCompanyId.HasValue)
                .Select(position => position.ChildCompanyId!.Value)
                .Distinct()
                .ToArray();

        IReadOnlyDictionary<int, MarketPriceSnapshot> currentPrices =
            new Dictionary<int, MarketPriceSnapshot>();

        if (
            isLatestPortfolio &&
            listedChildCompanyIds.Length > 0)
        {
            currentPrices =
                await marketPriceProvider
                    .GetLatestPricesAsync(
                        listedChildCompanyIds,
                        cancellationToken)
                    .ConfigureAwait(false);
        }

        var companyIdSet =
            childCompanyIds.ToHashSet();

        var companyRows =
            await dbContext.CompanyMaster
                .AsNoTracking()
                .Select(company => new
                {
                    company.CompanyId,
                    company.Symbol,
                    company.CompanyName,
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        var companies =
            companyRows
                .Where(company =>
                    companyIdSet.Contains(
                        company.CompanyId))
                .ToDictionary(company =>
                    company.CompanyId);

        var rows =
            positions
                .Select(position =>
                {
                    string? symbol = null;
                    string? companyName = null;
                    decimal? currentPrice = null;
                    decimal? currentValue = null;
                    string? currentPriceTradeDate = null;
                    bool usesCurrentMarketPrice = false;
                    if (
                        position.ChildCompanyId.HasValue &&
                        companies.TryGetValue(
                            position.ChildCompanyId.Value,
                            out var company))
                    {
                        symbol = company.Symbol;
                        companyName = company.CompanyName;
                    }
                    if (isLatestPortfolio)
                    {
                        if (
                            position.IsListed &&
                            position.ChildCompanyId.HasValue &&
                            currentPrices.TryGetValue(
                                position.ChildCompanyId.Value,
                                out var marketPrice))
                        {
                            currentPrice =
                                marketPrice.LastPrice ??
                                marketPrice.ClosingPrice;

                            currentPriceTradeDate =
                                marketPrice.TradeDate;

                            if (
                                currentPrice.HasValue &&
                                position.EndingQuantity.HasValue)
                            {
                                currentValue =
                                    position.EndingQuantity.Value *
                                    currentPrice.Value /
                                    1_000_000m;

                                usesCurrentMarketPrice = true;
                            }
                            else
                            {
                                currentValue =
                                    position.EndingCost;
                            }
                        }
                        else
                        {
                            currentValue =
                                position.EndingCost;
                        }
                    }
                    return new PortfolioPositionDto(
                        position.Id,
                        position.RowSequence,
                        position.ChildCompanyId,
                        position.RawCompanyName,
                        position.FSortName,
                        position.IsListed,
                        symbol,
                        companyName,
                        position.Capital,
                        position.NominalValue,
                        position.BeginningQuantity,
                        position.BeginningCost,
                        position.BeginningMarketValue,
                        position.ChangeQuantity,
                        position.ChangeCost,
                        position.ChangeMarketValue,
                        position.OwnershipPercent,
                        position.EndingQuantity,
                        position.EndingCost,
                        position.EndingMarketValue,
                        position.EndingCostPerShare,
                        position.EndingMarketPrice,
                        position.IncreaseDecrease,
                        position.Notes)
                    {
                        CurrentPrice = currentPrice,
                        CurrentValue = currentValue,
                        CurrentPriceTradeDate = currentPriceTradeDate,
                        UsesCurrentMarketPrice = usesCurrentMarketPrice,
                    };
                })
                .ToList();

        decimal? listedReportedMarketValue =
            positions
                .Where(position =>
                    position.IsListed &&
                    position.EndingMarketValue.HasValue)
                .Select(position =>
                    position.EndingMarketValue)
                .Sum();

        decimal? unlistedReportedValue =
            positions
                .Where(position =>
                    !position.IsListed &&
                    position.EndingCost.HasValue)
                .Select(position =>
                    position.EndingCost)
                .Sum();
        decimal? currentListedValue = isLatestPortfolio
                 ? rows.Where(row => row.IsListed &&
                                     row.CurrentValue.HasValue)
                       .Sum(row => row.CurrentValue)
                 : null;

        decimal? currentPortfolioValue = isLatestPortfolio
                  ? (currentListedValue ?? 0m) + (unlistedReportedValue ?? 0m)
                  : null;
        const decimal parentNominalValue = 1000m;

        decimal? currentPortfolioValuePerShare =
            currentPortfolioValue.HasValue &&
            registeredCapital.HasValue &&
            registeredCapital.Value > 0m
                ? (currentPortfolioValue.Value / registeredCapital.Value)
                    * parentNominalValue
                : null;

        logger.LogInformation("Portfolio handler returning response");
        return new PortfolioReportDto(
                  firstPosition.DisclosureId,
                  firstPosition.TracingNo,
                  parentCompanyId,
                  periodEndDate,
                  firstPosition.PublishDateTime,
                  (byte)sourceType,
                  (byte)auditStatus,
                  previousDisclosureId,
                  nextDisclosureId,
                  navigationTargets,
                  listedReportedMarketValue,
                  unlistedReportedValue,
                  rows)
        {
            IsLatestPortfolio = isLatestPortfolio,
            CurrentListedValue = currentListedValue,
            CurrentPortfolioValue = currentPortfolioValue,
            RegisteredCapital = registeredCapital,
            CurrentPortfolioValuePerShare = currentPortfolioValuePerShare,
        };
    }
}