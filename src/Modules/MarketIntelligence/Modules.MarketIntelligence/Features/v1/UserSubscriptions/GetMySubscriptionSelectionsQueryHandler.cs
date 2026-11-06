using FSH.Framework.Core.Context;
using FSH.Modules.MarketIntelligence.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MarketIntelligence.Features.v1.UserSubscriptions;

public sealed class GetMySubscriptionSelectionsQueryHandler(
    MarketIntelligenceDbContext dbContext,
    ICurrentUser currentUser)
    : IQueryHandler<
        GetMySubscriptionSelectionsQuery,
        GetMySubscriptionSelectionsResponse>
{
    public async ValueTask<GetMySubscriptionSelectionsResponse> Handle(
        GetMySubscriptionSelectionsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        string userId = currentUser.GetUserId().ToString();

        string tenantId = currentUser.GetTenant()
            ?? throw new InvalidOperationException(
                "Current tenant is required.");

        DateTime now = DateTime.UtcNow;

        string planKey = await dbContext.UserSubscriptions
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.UserId == userId &&
                x.StartUtc <= now &&
                (x.EndUtc == null || x.EndUtc > now))
            .OrderByDescending(x => x.StartUtc)
            .Select(x => x.PlanKey)
            .FirstOrDefaultAsync(cancellationToken)
            ?? "free";

        var limits = await dbContext.UserPlanLimits
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.PlanKey == planKey,
                cancellationToken)
            ?? throw new InvalidOperationException(
                $"Plan limits not found: {planKey}");

        int[] selected = await dbContext.UserSelectedSymbols
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.UserId == userId)
            .OrderBy(x => x.SelectedAtUtc)
            .ThenBy(x => x.CompanyId)
            .Select(x => x.CompanyId)
            .ToArrayAsync(cancellationToken);

        int[] valuationSelected =
            await dbContext.UserSelectedPortfolioValuationSymbols
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.UserId == userId)
                .OrderBy(x => x.SelectedAtUtc)
                .ThenBy(x => x.CompanyId)
                .Select(x => x.CompanyId)
                .ToArrayAsync(cancellationToken);
        var ids = selected.Concat(valuationSelected)
                  .Distinct()
                  .ToArray();

        var companies = await dbContext.CompanyMaster
            .AsNoTracking()
            .Where(x => ids.Contains(x.CompanyId))
            .Select(x => new
            {
                x.CompanyId,
                x.Symbol,
                x.CompanyName
            })
            .ToDictionaryAsync(x => x.CompanyId, cancellationToken);

        SubscriptionSelectedSymbolDto ToDto(int id) =>
            companies.TryGetValue(id, out var company)
                ? new(id, company.Symbol, company.CompanyName)
                : new(id, null, null);

        return new GetMySubscriptionSelectionsResponse(
            PlanKey: planKey,
            MaxSelectedSymbols: limits.MaxSelectedSymbols,
            MaxPortfolioValuationSymbols: limits.MaxPortfolioValuationSymbols,
            SelectedCompanyIds: selected.Select(ToDto).ToArray(),
            SelectedPortfolioValuationCompanyIds: valuationSelected.Select(ToDto).ToArray());
    }
}