using FSH.Framework.Shared.Persistence;
using FSH.Modules.MarketIntelligence.Contracts.Dtos;
using FSH.Modules.MarketIntelligence.Domain;
using FSH.Modules.MarketIntelligence.Contracts.v1.Disclosures;
using FSH.Modules.MarketIntelligence.Data;
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

        var q = dbContext.Disclosures.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search.Trim();
            q = q.Where(b =>
                EF.Functions.ILike(b.Name, $"%{term}%") ||
                EF.Functions.ILike(b.Slug, $"%{term}%"));
        }

        q = ApplySort(q, query.SortBy, query.SortDir);

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);
        var disclosures = await q
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<DisclosureDto>
        {
            Items = disclosures
                .Select(b => new DisclosureDto(b.Id, b.Name, b.Slug, b.Description, b.LogoUrl, b.CreatedAtUtc, b.UpdatedAtUtc, b.DeletedOnUtc, b.DeletedBy))
                .ToList(),
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size)
        };
    }

    // Whitelist + safe default: unknown columns/directions fall back to (name asc) so callers
    // can't trigger a server error or probe the entity shape via reflection-style sort keys.
    private static IQueryable<Disclosure> ApplySort(IQueryable<Disclosure> q, string? sortBy, string? sortDir)
    {
        bool desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        return (sortBy?.ToUpperInvariant()) switch
        {
            "SLUG" => desc ? q.OrderByDescending(b => b.Slug) : q.OrderBy(b => b.Slug),
            "CREATEDATUTC" or "CREATED" => desc
                ? q.OrderByDescending(b => b.CreatedAtUtc)
                : q.OrderBy(b => b.CreatedAtUtc),
            _ => desc ? q.OrderByDescending(b => b.Name) : q.OrderBy(b => b.Name),
        };
    }
}
