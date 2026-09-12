using FSH.Modules.MarketIntelligence.Contracts.v1.PortfolioMatching;
using FSH.Modules.MarketIntelligence.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Modules.MarketIntelligence.Features.v1.PortfolioMatching;

public sealed class GetUnmatchedPortfolioCompaniesQueryHandler(
    MarketIntelligenceDbContext dbContext)
    : IQueryHandler<
        GetUnmatchedPortfolioCompaniesQuery,
        IReadOnlyList<UnmatchedPortfolioCompanyDto>>
{
    private static readonly HashSet<string> AggregatePortfolioRows =[
    "سایرسهامپذیرفتهشدهدربورسوفرابورس",
    "سایرشرکتهایخارجازبورس",
    "سایرشرکتهایپذیرفتهشدهدربورس",
    "مشارکتهایمدنیخارجازبورس",
    "مشارکتهایمدنی)خارجازبورس(",
    "اوراقمشارکتپذیرفتهشدهدربورس",
    "اوراقمشارکت(پذیرفتهشدهدربورس)",
    "(حقتقدم)",
    "دراوراقبهاداربادرآمدثابتکاریزما",
    "سایرسهامدرجشدهدربازارهایپایهفرابورس"];

    public async ValueTask<IReadOnlyList<UnmatchedPortfolioCompanyDto>> Handle(
        GetUnmatchedPortfolioCompaniesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var unmatchedPositions =
            await dbContext.InvestmentPortfolioPositions
                .AsNoTracking()
                .Where(position =>
                    position.ChildCompanyId == null &&
                    !AggregatePortfolioRows.Contains(position.FSortName))
                .Select(position => new
                {
                    position.RawCompanyName,
                    position.FSortName,
                    position.IsListed,
                    position.ParentCompanyId,
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        IReadOnlyList<UnmatchedPortfolioCompanyDto> result =
            unmatchedPositions
                .GroupBy(position => new
                {
                    position.FSortName,
                    position.IsListed,
                })
                .Select(group =>
                {
                    string rawCompanyName =
                        group
                            .GroupBy(position => position.RawCompanyName)
                            .OrderByDescending(rawGroup => rawGroup.Count())
                            .ThenBy(rawGroup => rawGroup.Key)
                            .Select(rawGroup => rawGroup.Key)
                            .First();

                    return new UnmatchedPortfolioCompanyDto(
                        rawCompanyName,
                        group.Key.FSortName,
                        group.Key.IsListed,
                        group.Count(),
                        group.Select(position => position.ParentCompanyId)
                            .Distinct()
                            .Count());
                })
                .OrderByDescending(item => item.ParentSymbolCount)
                .ThenByDescending(item => item.OccurrenceCount)
                .ThenBy(item => item.RawCompanyName)
                .ToList();

        return result;
    }
}