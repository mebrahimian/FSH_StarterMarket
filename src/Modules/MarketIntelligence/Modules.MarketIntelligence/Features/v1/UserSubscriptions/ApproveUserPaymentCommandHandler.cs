using FSH.Framework.Core.Context;
using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Domain;
using FSH.Modules.MarketIntelligence.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Data;

namespace FSH.Modules.MarketIntelligence.Features.v1.UserSubscriptions;

public sealed class ApproveUserPaymentCommandHandler(
    MarketIntelligenceDbContext dbContext,
    ICurrentUser currentUser,
    IServiceScopeFactory scopeFactory)
    : ICommandHandler<ApproveUserPaymentCommand>
{
    public async ValueTask<Unit> Handle(
        ApproveUserPaymentCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!currentUser.IsAuthenticated() ||
            currentUser.GetTenant() != "marketintelligence")
            throw new UnauthorizedAccessException();

        var tenantId = currentUser.GetTenant();
        var reviewerId = currentUser.GetUserId().ToString();
        var now = DateTime.UtcNow;

        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var scope = scopeFactory.CreateAsyncScope();

            var retryDb = scope.ServiceProvider
                .GetRequiredService<MarketIntelligenceDbContext>();

            await using var transaction =
                await retryDb.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

            var payment = await retryDb.UserPaymentRequests
                .SingleOrDefaultAsync(
                    x => x.Id == command.PaymentRequestId &&
                         x.TenantId == tenantId,
                    cancellationToken);

            if (payment is null)
                throw new InvalidOperationException("Payment request not found.");

            if (payment.Status == UserPaymentStatus.Approved)
            {
                var subscriptionExists = await retryDb.UserSubscriptions
                    .AnyAsync(x =>
                        x.TenantId == payment.TenantId &&
                        x.UserId == payment.UserId &&
                        x.PlanKey == payment.PlanKey &&
                        x.PlanKey != "free" &&
                        x.StartUtc <= now &&
                        (x.EndUtc == null || x.EndUtc > now),
                        cancellationToken);

                if (!subscriptionExists)
                    throw new InvalidOperationException(
                        "Payment is approved but no matching active subscription exists.");

                return;
            }

            var creditOnlyUpgrade = payment.RequestType == UserPaymentRequestType.Upgrade &&
                                       payment.Amount == 0m &&
                                       command.PaidAmount == 0m;
            if (payment.Status != UserPaymentStatus.Pending ||
                (payment.ReceiptFileId is null && !creditOnlyUpgrade))
            {
                throw new InvalidOperationException(
                    "Payment request cannot be approved.");
            }

            var activeSubscription = await retryDb.UserSubscriptions
                .SingleOrDefaultAsync(
                   x => x.TenantId == tenantId &&
                        x.UserId == payment.UserId &&
                        x.PlanKey != "free" &&
                        x.StartUtc <= now &&
                        (x.EndUtc == null || x.EndUtc > now),
                         cancellationToken);

            DateTime startUtc;

            switch (payment.RequestType)
            {
                case UserPaymentRequestType.Purchase:
                    if (activeSubscription is not null)
                        throw new InvalidOperationException(
                            "User already has an active paid subscription.");

                    startUtc = now;
                    break;

                case UserPaymentRequestType.Renewal:
                    if (activeSubscription is null ||
                        activeSubscription.PlanKey != payment.PlanKey ||
                        activeSubscription.EndUtc is null)
                    {
                        throw new InvalidOperationException(
                            "Renewal requires an active subscription of the same plan.");
                    }

                    var alreadyScheduled = await retryDb.UserSubscriptions
                        .AnyAsync(
                            x => x.TenantId == tenantId &&
                                 x.UserId == payment.UserId &&
                                 x.StartUtc >= activeSubscription.EndUtc.Value,
                            cancellationToken);

                    if (alreadyScheduled)
                        throw new InvalidOperationException(
                            "A future subscription is already scheduled.");

                    startUtc = activeSubscription.EndUtc.Value;
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

                        var targetRank = payment.PlanKey switch
                        {
                            "economic" => 1,
                            "professional" => 2,
                            "premium" => 3,
                            _ => 0
                        };

                        if (currentRank == 0 || targetRank <= currentRank)
                            throw new InvalidOperationException(
                                "Upgrade requires a higher plan.");

                        if (command.PaidAmount is null)
                            throw new InvalidOperationException(
                                "Confirmed paid amount is required for upgrade.");

                        var hasFutureSubscription = await retryDb.UserSubscriptions
                            .AnyAsync(x =>
                                x.TenantId == tenantId &&
                                x.UserId == payment.UserId &&
                                x.PlanKey != "free" &&
                                x.StartUtc >= now,
                                cancellationToken);

                        if (hasFutureSubscription)
                            throw new InvalidOperationException(
                                "Upgrade with a scheduled subscription is not supported.");

                        var sourcePayment = await retryDb.UserPaymentRequests
                            .AsNoTracking()
                            .SingleOrDefaultAsync(x =>
                                x.Id == sourcePaymentId &&
                                x.TenantId == tenantId &&
                                x.UserId == payment.UserId &&
                                x.Status == UserPaymentStatus.Approved,
                                cancellationToken);

                        if (sourcePayment is null)
                            throw new InvalidOperationException(
                                "Approved source payment not found.");

                        var remainingCredit = sourcePayment.CalculateRemainingCredit(
                            activeSubscription.StartUtc,
                            activeSubscription.EndUtc.Value,
                            now);

                        payment.RecalculateUpgradeCredit(remainingCredit);
                        if (payment.ReceiptFileId is null &&
                           (payment.Amount > 0m || command.PaidAmount != 0m))
                        {
                            throw new InvalidOperationException(
                                "Upgrade amount has changed. Payment receipt is required.");
                        }
                        activeSubscription.EndAt(now);

                        startUtc = now;
                        break;
                    }

                default:
                    throw new InvalidOperationException(
                        "Invalid payment request type.");
            }

            var paidAmount = command.PaidAmount ?? payment.Amount;

            if (paidAmount < 0m)
                throw new InvalidOperationException(
                    "Paid amount cannot be negative.");

            var totalCredit = paidAmount + payment.AppliedCredit;

            if (totalCredit <= 0m)
                throw new InvalidOperationException(
                    "Subscription credit must be greater than zero.");

            // تعداد روزهای واقعی دوره انتخاب‌شده
            var standardEndUtc = startUtc.AddMonths(payment.DurationMonths);
            var standardDays = (standardEndUtc - startUtc).Days;

            // تبدیل مبلغ پرداختی و اعتبار قبلی به روز
            var creditedDays = (int)Math.Ceiling(
                standardDays * totalCredit / payment.OriginalPrice);

            // تاریخ پایان نهایی
            var endUtc = startUtc.AddDays(creditedDays);

            var subscription = UserSubscription.Create(
                payment.TenantId,
                payment.UserId,
                payment.PlanKey,
                startUtc,
                endUtc,
                paymentRequestId: payment.Id);

            payment.Approve(reviewerId, now, command.PaidAmount);
            retryDb.UserSubscriptions.Add(subscription);

            await retryDb.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });

        return Unit.Value;
    }
}