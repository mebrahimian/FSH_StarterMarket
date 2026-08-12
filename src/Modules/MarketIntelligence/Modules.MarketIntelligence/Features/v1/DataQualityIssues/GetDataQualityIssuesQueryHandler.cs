using FSH.Modules.MarketIntelligence.Contracts.Dtos;
using FSH.Modules.MarketIntelligence.Contracts.v1.DataQualityIssues;
using FSH.Modules.MarketIntelligence.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;
using FSH.Framework.Shared.Utilities;

namespace Modules.MarketIntelligence.Features.v1.DataQualityIssues;

public sealed class GetDataQualityIssuesQueryHandler(
    MarketIntelligenceDbContext dbContext)
    : IQueryHandler<
        GetDataQualityIssuesQuery,
        IReadOnlyList<DataQualityIssueDto>>
{
    public async ValueTask<IReadOnlyList<DataQualityIssueDto>> Handle(
        GetDataQualityIssuesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var summaries = await (
        from summary in dbContext.MonthlyActivitySummaries
            .AsNoTracking()

        join disclosure in dbContext.Disclosures
            .AsNoTracking()
            on summary.DisclosureId equals disclosure.Id
            into disclosureGroup

        from disclosure in disclosureGroup.DefaultIfEmpty()

        where summary.YearEndDate != null

        orderby
            summary.Symbol,
            summary.YearEndDate,
            summary.PeriodEndDate

        select new
        {
            summary.Symbol,
            YearEndDate = summary.YearEndDate!,
            summary.PeriodEndDate,
            summary.PreviousYearToDateAmount,

            PublishDateTimeRaw =
                disclosure != null
                    ? disclosure.PublishDateTimeRaw
                    : null,
        })
    .ToListAsync(cancellationToken)
    .ConfigureAwait(false);

        var issues = new List<DataQualityIssueDto>();

        foreach (var group in summaries.GroupBy(summary => new
        {
            summary.Symbol,
            summary.YearEndDate,
        }))
        {
            decimal? previousValue = null;

            foreach (var summary in group)
            {
                decimal? currentValue =
                    summary.PreviousYearToDateAmount;

                if (previousValue.HasValue &&
                    currentValue.HasValue &&
                    currentValue.Value < previousValue.Value)
                {
                    string? publishDate = PersianDateTextParser
                        .TryExtract(summary.PublishDateTimeRaw);

                    issues.Add(
                        new DataQualityIssueDto(
                            summary.Symbol,
                            summary.YearEndDate,
                            summary.PeriodEndDate,
                            publishDate,
                            "PreviousYearYtdRegression",
                            previousValue,
                            currentValue));
                }

                previousValue = currentValue;
            }
        }

        return issues;
    }
}