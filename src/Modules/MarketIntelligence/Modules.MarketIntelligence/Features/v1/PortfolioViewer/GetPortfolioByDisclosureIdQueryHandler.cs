using FSH.Modules.MarketIntelligence.Contracts.v1.PortfolioViewer;
using FSH.Modules.MarketIntelligence.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Modules.MarketIntelligence.Features.v1.PortfolioViewer;

public sealed class GetPortfolioByDisclosureIdQueryHandler(
    MarketIntelligenceDbContext dbContext,
    ILogger<GetPortfolioByDisclosureIdQueryHandler> logger)
    : IQueryHandler<
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

        int parentCompanyId =
            firstPosition.ParentCompanyId;

        string periodEndDate =
            firstPosition.PeriodEndDate;

        var reportPeriods =
            await dbContext.InvestmentPortfolioPositions
                .AsNoTracking()
                .Where(position =>
                    position.ParentCompanyId == parentCompanyId)
                .Select(position => new
                {
                    position.DisclosureId,
                    position.PeriodEndDate,
                })
                .Distinct()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        logger.LogInformation("Portfolio handler started");
        var orderedReports =
            reportPeriods
                .OrderBy(report =>
                    report.PeriodEndDate,
                    StringComparer.Ordinal)
                .ToList();

        int currentIndex =
            orderedReports.FindIndex(report =>
                report.DisclosureId ==
                query.DisclosureId);

        Guid? previousDisclosureId =
            currentIndex > 0
                ? orderedReports[currentIndex - 1]
                    .DisclosureId
                : null;

        Guid? nextDisclosureId =
            currentIndex >= 0 &&
            currentIndex < orderedReports.Count - 1
                ? orderedReports[currentIndex + 1]
                    .DisclosureId
                : null;

        int[] childCompanyIds =
            positions
                .Where(position =>
                    position.ChildCompanyId.HasValue)
                .Select(position =>
                    position.ChildCompanyId!.Value)
                .Distinct()
                .ToArray();

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

                    if (
                        position.ChildCompanyId.HasValue &&
                        companies.TryGetValue(
                            position.ChildCompanyId.Value,
                            out var company))
                    {
                        symbol = company.Symbol;
                        companyName = company.CompanyName;
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
                        position.Notes);
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
        logger.LogInformation("Portfolio handler returning response");
        return new PortfolioReportDto(
            firstPosition.DisclosureId,
            firstPosition.TracingNo,
            parentCompanyId,
            periodEndDate,
            firstPosition.PublishDateTime,
            previousDisclosureId,
            nextDisclosureId,
            listedReportedMarketValue,
            unlistedReportedValue,
            rows);
    }
}