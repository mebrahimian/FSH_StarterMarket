using FSH.Modules.Billing.Contracts.v1.Plans;
using FSH.Modules.MarketIntelligence.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Modules.MarketIntelligence.Data;

namespace FSH.Modules.MarketIntelligence.Features.v1.UserSubscriptions;

public sealed class GetSubscriptionPlansQueryHandler(
    MarketIntelligenceDbContext dbContext,
    IMediator mediator)
    : IQueryHandler<
        GetSubscriptionPlansQuery,
        IReadOnlyList<SubscriptionPlanResponse>>
{
    public async ValueTask<IReadOnlyList<SubscriptionPlanResponse>> Handle(
        GetSubscriptionPlansQuery query,
        CancellationToken cancellationToken)
    {
        var billingPlans = await mediator.Send(
            new GetPlansQuery(),
            cancellationToken);

        var terms = await dbContext.UserSubscriptionTerms
            .AsNoTracking()
            .OrderBy(x => x.DurationMonths)
            .ToListAsync(cancellationToken);

        var limits = await dbContext.UserPlanLimits
            .AsNoTracking()
            .Select(x => x.PlanKey)
            .ToListAsync(cancellationToken);

        var allowedPlans = new HashSet<string>(
            limits,
            StringComparer.OrdinalIgnoreCase);

        IReadOnlyList<SubscriptionPlanResponse> result = billingPlans
            .Where(p => p.IsActive && allowedPlans.Contains(p.Key))
            .Select(p => new SubscriptionPlanResponse(
                PlanKey: p.Key,
                Currency: p.Currency,
                MonthlyBasePrice: p.MonthlyBasePrice,
                Terms: terms.Select(t => new SubscriptionTermResponse(
                    DurationMonths: t.DurationMonths,
                    DiscountPercent: t.DiscountPercent,
                    FinalPrice: decimal.Round(
                        p.MonthlyBasePrice *
                        t.DurationMonths *
                        (1m - t.DiscountPercent / 100m),
                        0,
                        MidpointRounding.AwayFromZero)
                )).ToList()
            ))
            .ToList();

        return result;
    }
}