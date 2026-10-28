using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MarketIntelligence.Services.Portfolio;

public sealed record PortfolioValuationReportRef(
    int CompanyId,
    string PeriodEndDate,
    Guid DisclosureId,
    long TracingNo,
    DateTime? PublishDateTime);

public static class PortfolioValuationReportSelector
{
    public static async Task<
        IReadOnlyList<PortfolioValuationReportRef>> GetReportsAsync(
        MarketIntelligenceDbContext dbContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        var reports = await dbContext.InvestmentPortfolioPositions
            .AsNoTracking()
            .Where(p =>
                p.SourceType == PortfolioSourceType.MonthlyActivity)
            .Select(p => new
            {
                CompanyId = p.ParentCompanyId,
                p.PeriodEndDate,
                p.DisclosureId,
                p.TracingNo,
                p.PublishDateTime
            })
            .Distinct()
            .ToListAsync(cancellationToken);

        return reports
            .GroupBy(p => new
            {
                p.CompanyId,
                p.PeriodEndDate
            })
            .Select(group => group
                .OrderByDescending(p => p.PublishDateTime)
                .ThenByDescending(p => p.TracingNo)
                .First())
            .OrderBy(p => p.CompanyId)
            .ThenBy(p => p.PeriodEndDate)
            .Select(p => new PortfolioValuationReportRef(
                p.CompanyId,
                p.PeriodEndDate,
                p.DisclosureId,
                p.TracingNo,
                p.PublishDateTime))
            .ToArray();
    }
    public static async Task<IReadOnlyList<PortfolioValuationReportRef>>
    GetReportRevisionsAsync(
        MarketIntelligenceDbContext dbContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        var rows = await dbContext.InvestmentPortfolioPositions
            .AsNoTracking()
            .Where(p =>
                p.SourceType == PortfolioSourceType.MonthlyActivity &&
                p.PublishDateTime.HasValue)
            .Select(p => new
            {
                CompanyId = p.ParentCompanyId,
                p.PeriodEndDate,
                p.DisclosureId,
                p.TracingNo,
                p.PublishDateTime
            })
            .Distinct()
            .ToListAsync(cancellationToken);

        return rows
            .OrderBy(x => x.CompanyId)
            .ThenBy(x => x.PeriodEndDate)
            .ThenBy(x => x.PublishDateTime)
            .ThenBy(x => x.TracingNo)
            .Select(x => new PortfolioValuationReportRef(
                x.CompanyId,
                x.PeriodEndDate,
                x.DisclosureId,
                x.TracingNo,
                x.PublishDateTime))
            .ToArray();
    }
}