using FSH.Framework.Core.Context;
using FSH.Modules.MarketIntelligence.Data;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MarketIntelligence.Services.Subscriptions;

public sealed record LatestDataAccessScope(
    bool AllCompanies,
    IReadOnlySet<int> AllowedCompanyIds)
{
    public bool CanAccessLatest(int companyId) =>
        AllCompanies || AllowedCompanyIds.Contains(companyId);
}

public sealed class LatestDataAccessService(
    MarketIntelligenceDbContext dbContext,
    ICurrentUser currentUser)
{
    public async Task<LatestDataAccessScope> GetScopeAsync(
        CancellationToken cancellationToken = default)
    {
        string userId = currentUser.GetUserId().ToString();

        string tenantId = currentUser.GetTenant()
            ?? throw new InvalidOperationException(
                "Current tenant is required.");

        DateTime now = DateTime.UtcNow;

        string? activePlanKey = await dbContext.UserSubscriptions
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.UserId == userId &&
                x.StartUtc <= now &&
                (x.EndUtc == null || x.EndUtc > now))
            .OrderByDescending(x => x.StartUtc)
            .Select(x => x.PlanKey)
            .FirstOrDefaultAsync(cancellationToken);

        string planKey = activePlanKey ?? "free";

        int? maxSymbols = await dbContext.UserPlanLimits
            .AsNoTracking()
            .Where(x => x.PlanKey == planKey)
            .Select(x => (int?)x.MaxSelectedSymbols)
            .FirstOrDefaultAsync(cancellationToken);

        if (!maxSymbols.HasValue)
        {
            throw new InvalidOperationException(
                $"Plan limits not found: {planKey}");
        }

        // -1 means access to the entire market.
        if (maxSymbols.Value == -1)
        {
            return new LatestDataAccessScope(
                true,
                new HashSet<int>());
        }

        int[] companyIds = await dbContext.UserSelectedSymbols
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.UserId == userId)
            .OrderBy(x => x.SelectedAtUtc)
            .ThenBy(x => x.CompanyId)
            .Select(x => x.CompanyId)
            .Take(maxSymbols.Value)
            .ToArrayAsync(cancellationToken);

        return new LatestDataAccessScope(
            false,
            companyIds.ToHashSet());
    }
}