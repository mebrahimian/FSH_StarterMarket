using FSH.Framework.Core.Context;
using FSH.Modules.MarketIntelligence.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Modules.MarketIntelligence.Data;

namespace FSH.Modules.MarketIntelligence.Features.v1.UserSubscriptions;

public sealed class GetMySubscriptionQueryHandler(
    MarketIntelligenceDbContext dbContext,
    ICurrentUser currentUser)
    : IQueryHandler<GetMySubscriptionQuery, GetMySubscriptionResponse>
{
    public async ValueTask<GetMySubscriptionResponse> Handle(
        GetMySubscriptionQuery query,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId().ToString();
        var tenantId = currentUser.GetTenant()
            ?? throw new InvalidOperationException(
                "Current tenant is required.");

        var now = DateTime.UtcNow;

        var subscription = await dbContext.UserSubscriptions
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.UserId == userId &&
                x.StartUtc <= now &&
                (x.EndUtc == null || x.EndUtc > now))
            .OrderByDescending(x => x.StartUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var planKey = subscription?.PlanKey ?? "free";

        var limits = await dbContext.UserPlanLimits
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.PlanKey == planKey,
                cancellationToken);

        if (limits is null)
        {
            throw new InvalidOperationException(
                $"Plan limits not found: {planKey}");
        }

        var selectedCount = await dbContext.UserSelectedSymbols
            .AsNoTracking()
            .CountAsync(
                x => x.TenantId == tenantId &&
                     x.UserId == userId,
                cancellationToken);

        return new GetMySubscriptionResponse(
            PlanKey: planKey,
            MaxSelectedSymbols: limits.MaxSelectedSymbols,
            MaxSymbolsPerIndustry: limits.MaxSymbolsPerIndustry,
            SelectedSymbolsCount: selectedCount,
            StartUtc: subscription?.StartUtc,
            EndUtc: subscription?.EndUtc,
            IsFree: planKey == "free");
    }
}