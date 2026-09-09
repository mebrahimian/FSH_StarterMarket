using FSH.Framework.Shared.Persistence;
using FSH.Modules.MarketIntelligence.Contracts.Dtos;
using FSH.Modules.MarketIntelligence.Contracts.v1.Disclosures;
using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Domain;
using FSH.Modules.MarketIntelligence.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MarketIntelligence.Features.v1.Disclosures.SearchDisclosures;

public sealed class SearchDisclosuresQueryHandler(MarketIntelligenceDbContext dbContext)
    : IQueryHandler<SearchDisclosuresQuery, PagedResponse<DisclosureDto>>
{
    public async ValueTask<PagedResponse<DisclosureDto>> Handle(SearchDisclosuresQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 20 : query.PageSize;

        IQueryable<Disclosure> q =
    dbContext.Disclosures.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search.Trim();

            q = q.Where(disclosure =>
                disclosure.Symbol.Contains(term) ||
                disclosure.CompanyName.Contains(term) ||
                disclosure.Title.Contains(term) ||
                disclosure.LetterCode.Contains(term));
        }

        if (query.Lets is { Length: > 0 })
        {
            short[] letCodes = query.Lets;

            if (query.IncludeNullLet)
            {
                q = q.Where(disclosure =>
                    !disclosure.Let.HasValue ||
                    letCodes.Contains(
                        disclosure.Let.Value));
            }
            else
            {
                q = q.Where(disclosure =>
                    disclosure.Let.HasValue &&
                    letCodes.Contains(
                        disclosure.Let.Value));
            }
        }
        else if (query.IncludeNullLet)
        {
            q = q.Where(disclosure =>
                !disclosure.Let.HasValue);
        }
        else if (query.Let.HasValue)
        {
            // پشتیبانی موقت از فیلتر قدیمی تک‌کدی
            q = q.Where(disclosure =>
                disclosure.Let == query.Let.Value);
        }

        if (query.Rt.HasValue)
        {
            q = q.Where(disclosure =>
                disclosure.Rt == query.Rt.Value);
        }

        if (query.ReportingTypeCode.HasValue)
        {
            q = q.Where(disclosure =>
                disclosure.ReportingTypeCode ==
                query.ReportingTypeCode.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.SalesParseStatus) &&
            System.Enum.TryParse(
                query.SalesParseStatus,
                ignoreCase: true,
                out DisclosureParseStatus parseStatus))
        {
            q = q.Where(disclosure =>
                disclosure.SalesParseStatus == parseStatus);
        }

        q = ApplySort(
            q,
            query.SortBy,
            query.SortDir);

        q = ApplySort(q, query.SortBy, query.SortDir);

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);
        var disclosures = await q
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        List<Guid> disclosureIds =  disclosures.Select(x => x.Id).ToList();

        HashSet<Guid> portfolioDisclosureIds =
            await dbContext.InvestmentPortfolioReportMetadata
                .AsNoTracking()
                .Where(x => disclosureIds.Contains(x.DisclosureId))
                .Select(x => x.DisclosureId)
                .Distinct()
                .ToHashSetAsync(cancellationToken)
                .ConfigureAwait(false);

        return new PagedResponse<DisclosureDto>
        {
            Items = disclosures.Select(disclosure => new DisclosureDto(
                Id: disclosure.Id,
                TracingNo: disclosure.TracingNo,
                Symbol: disclosure.Symbol,
                CompanyName: disclosure.CompanyName,
                Title: disclosure.Title,
                LetterCode: disclosure.LetterCode,
                SentDateTimeRaw: disclosure.SentDateTimeRaw,
                PublishDateTimeRaw: disclosure.PublishDateTimeRaw,
                SentDateTime: disclosure.SentDateTime,
                PublishDateTime: disclosure.PublishDateTime,
                HasHtml: disclosure.HasHtml,
                IsEstimate: disclosure.IsEstimate,
                Url: disclosure.Url,
                HasExcel: disclosure.HasExcel,
                HasPdf: disclosure.HasPdf,
                HasXbrl: disclosure.HasXbrl,
                HasAttachment: disclosure.HasAttachment,
                AttachmentUrl: disclosure.AttachmentUrl,
                PdfUrl: disclosure.PdfUrl,
                ExcelUrl: disclosure.ExcelUrl,
                XbrlUrl: disclosure.XbrlUrl,
                TedanUrl: disclosure.TedanUrl,
                Let: disclosure.Let,
                Rt: disclosure.Rt,
                Ct: disclosure.Ct,
                Ft: disclosure.Ft,
                ReportingTypeCode: disclosure.ReportingTypeCode,
                SalesParseStatus: disclosure.SalesParseStatus.ToString(),
                SalesParsedAt: disclosure.SalesParsedAt,
                HasPortfolio: portfolioDisclosureIds.Contains(disclosure.Id)))
            .ToList(),
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size)
        };
    }

    // Whitelist + safe default: unknown columns/directions fall back to (name asc) so callers
    // can't trigger a server error or probe the entity shape via reflection-style sort keys.
    private static IQueryable<Disclosure> ApplySort(
    IQueryable<Disclosure> query,
    string? sortBy,
    string? sortDir)
    {
        string normalizedSortBy =
            sortBy?.Trim().ToUpperInvariant() ??
            "PUBLISHDATETIME";

        bool descending =
            string.IsNullOrWhiteSpace(sortDir) ||
            string.Equals(
                sortDir,
                "desc",
                StringComparison.OrdinalIgnoreCase);

        return normalizedSortBy switch
        {
            "TRACINGNO" => descending
                ? query
                    .OrderByDescending(disclosure => disclosure.TracingNo)
                : query
                    .OrderBy(disclosure => disclosure.TracingNo),

            "SYMBOL" => descending
                ? query
                    .OrderByDescending(disclosure => disclosure.Symbol)
                    .ThenByDescending(disclosure => disclosure.TracingNo)
                : query
                    .OrderBy(disclosure => disclosure.Symbol)
                    .ThenByDescending(disclosure => disclosure.TracingNo),

            "COMPANYNAME" => descending
                ? query
                    .OrderByDescending(disclosure => disclosure.CompanyName)
                    .ThenByDescending(disclosure => disclosure.TracingNo)
                : query
                    .OrderBy(disclosure => disclosure.CompanyName)
                    .ThenByDescending(disclosure => disclosure.TracingNo),

            "SENTDATETIME" => descending
                ? query
                    .OrderByDescending(disclosure => disclosure.SentDateTime)
                    .ThenByDescending(disclosure => disclosure.TracingNo)
                : query
                    .OrderBy(disclosure => disclosure.SentDateTime)
                    .ThenByDescending(disclosure => disclosure.TracingNo),

            "SALESPARSEDAT" => descending
                ? query
                    .OrderByDescending(disclosure => disclosure.SalesParsedAt)
                    .ThenByDescending(disclosure => disclosure.TracingNo)
                : query
                    .OrderBy(disclosure => disclosure.SalesParsedAt)
                    .ThenByDescending(disclosure => disclosure.TracingNo),

            _ => descending
                ? query
                    .OrderByDescending(disclosure => disclosure.PublishDateTime)
                    .ThenByDescending(disclosure => disclosure.TracingNo)
                : query
                    .OrderBy(disclosure => disclosure.PublishDateTime)
                    .ThenByDescending(disclosure => disclosure.TracingNo)
        };
    }
}
