
using FSH.Modules.Billing.Contracts.v1.Plans;
using FSH.Modules.MarketIntelligence.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;
using FSH.Framework.Core.Context;
using FSH.Modules.MarketIntelligence.Domain;
using FSH.Modules.MarketIntelligence.Domain.Enums;

namespace FSH.Modules.MarketIntelligence.Features.v1.UserSubscriptions;

public sealed class CreateUserPaymentRequestCommandHandler(
    MarketIntelligenceDbContext dbContext,
    IMediator mediator,
    ICurrentUser currentUser) : ICommandHandler<CreateUserPaymentRequestCommand, Guid>
{
    public async ValueTask<Guid> Handle(
        CreateUserPaymentRequestCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        
        if (!currentUser.IsAuthenticated() || currentUser.GetTenant() != "marketintelligence")
        {
            throw new UnauthorizedAccessException();
        }

        var tenantId = currentUser.GetTenant();
        var userId = currentUser.GetUserId().ToString();
        if (command.DurationMonths is not (1 or 3 or 6 or 12))
            throw new ArgumentOutOfRangeException(
                nameof(command),
                "Invalid subscription duration.");

        // UserId را باید از هویت احرازشده دریافت کنیم؛
        // هرگز از اطلاعات ارسالی کاربر استفاده نمی‌کنیم.

        var billingPlans = await mediator.Send(
            new GetPlansQuery(),
            cancellationToken);

        var plan = billingPlans.SingleOrDefault(p =>
            p.IsActive &&
            p.Key == command.PlanKey &&
            p.Key != "free");

        if (plan is null)
            throw new InvalidOperationException("Invalid subscription plan.");

        var planIsAllowed = await dbContext.UserPlanLimits
            .AnyAsync(x => x.PlanKey == plan.Key, cancellationToken);

        if (!planIsAllowed)
            throw new InvalidOperationException("Subscription plan is not allowed.");

        var term = await dbContext.UserSubscriptionTerms
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.DurationMonths == command.DurationMonths,
                cancellationToken);

        if (term is null)
            throw new InvalidOperationException("Subscription term not found.");

        var originalPrice = decimal.Round(
            plan.MonthlyBasePrice *
            command.DurationMonths *
            (1m - term.DiscountPercent / 100m),
            0,
            MidpointRounding.AwayFromZero);
        ArgumentNullException.ThrowIfNull(tenantId);

        var now = DateTime.UtcNow;

        var activeSubscription = await dbContext.UserSubscriptions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.UserId == userId &&
                     x.PlanKey != "free" &&
                     x.StartUtc <= now &&
                     (x.EndUtc == null || x.EndUtc > now),
                cancellationToken);
        decimal remainingCredit = 0m;
        decimal upgradeFee = 0m;
        decimal appliedCredit = 0m;
        switch (command.RequestType)
        {
            case UserPaymentRequestType.Purchase:
                if (activeSubscription is not null)
                    throw new InvalidOperationException(
                        "An active paid subscription exists.");
                break;

            case UserPaymentRequestType.Renewal:
                if (activeSubscription is null ||
                    activeSubscription.PlanKey != plan.Key ||
                    activeSubscription.EndUtc is null)
                    throw new InvalidOperationException(
                        "Renewal requires an active subscription of the same plan.");
                break;

            case UserPaymentRequestType.Upgrade:
                {
                    if (activeSubscription is null ||
                        activeSubscription.EndUtc is null ||
                        activeSubscription.PaymentRequestId is not Guid sourcePaymentId)
                    {
                        throw new InvalidOperationException(
                            "Active subscription or source payment not found.");
                    }

                    var currentRank = activeSubscription.PlanKey switch
                    {
                        "economic" => 1,
                        "professional" => 2,
                        "premium" => 3,
                        _ => 0
                    };

                    var targetRank = plan.Key switch
                    {
                        "economic" => 1,
                        "professional" => 2,
                        "premium" => 3,
                        _ => 0
                    };

                    if (currentRank == 0 || targetRank <= currentRank)
                        throw new InvalidOperationException(
                            "Upgrade requires a higher plan.");

                    var hasFutureSubscription = await dbContext.UserSubscriptions
                        .AnyAsync(x =>
                            x.TenantId == tenantId &&
                            x.UserId == userId &&
                            x.PlanKey != "free" &&
                            x.StartUtc >= now,
                            cancellationToken);

                    if (hasFutureSubscription)
                        throw new InvalidOperationException(
                            "Upgrade with a scheduled subscription is not supported.");

                    var sourcePayment = await dbContext.UserPaymentRequests
                        .AsNoTracking()
                        .SingleOrDefaultAsync(x =>
                            x.Id == sourcePaymentId &&
                            x.TenantId == tenantId &&
                            x.UserId == userId &&
                            x.Status == UserPaymentStatus.Approved,
                            cancellationToken);

                    if (sourcePayment is null)
                        throw new InvalidOperationException(
                            "Approved source payment not found.");

                    remainingCredit = sourcePayment.CalculateRemainingCredit(
                        activeSubscription.StartUtc,
                        activeSubscription.EndUtc.Value,
                        now);

                    upgradeFee = decimal.Round(
                        remainingCredit * 0.25m,
                        0,
                        MidpointRounding.AwayFromZero);

                    appliedCredit = remainingCredit - upgradeFee;

                    break;
                }

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(command),
                    command.RequestType,
                    "Invalid payment request type.");
        }
        var hasPendingRequest = await dbContext.UserPaymentRequests
            .AnyAsync(x => x.TenantId == tenantId &&
                           x.UserId == userId &&
                           x.Status == UserPaymentStatus.Pending,
                           cancellationToken);

        if (hasPendingRequest)
        {
            throw new InvalidOperationException(
                "You already have a pending payment request.");
        }
        var paymentRequest = UserPaymentRequest.Create(
            tenantId: tenantId,
            userId: userId,
            planKey: plan.Key,
            durationMonths: command.DurationMonths,
            discountPercent: term.DiscountPercent,
            originalPrice: originalPrice,
            remainingCredit: remainingCredit,
            upgradeFee: upgradeFee,
            appliedCredit: appliedCredit,
            amount: Math.Max(0m, originalPrice - appliedCredit),
            currency: plan.Currency,
            requestType: command.RequestType);

        dbContext.UserPaymentRequests.Add(paymentRequest);

        await dbContext.SaveChangesAsync(cancellationToken);

        return paymentRequest.Id;
    }


}
