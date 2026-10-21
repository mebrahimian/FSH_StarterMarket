
using FSH.Framework.Core.Context;
using Mediator;
using Microsoft.EntityFrameworkCore;
using FSH.Modules.MarketIntelligence.Domain;
using FSH.Modules.MarketIntelligence.Data;

namespace FSH.Modules.MarketIntelligence.Features.v1.UserSubscriptions;

public sealed class GetMyPendingPaymentRequestQueryHandler(
    MarketIntelligenceDbContext dbContext,
    ICurrentUser currentUser)
    : IQueryHandler<
        GetMyPendingPaymentRequestQuery,
        MyPendingPaymentRequestResponse?>
{
    public async ValueTask<MyPendingPaymentRequestResponse?> Handle(
        GetMyPendingPaymentRequestQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!currentUser.IsAuthenticated() ||
            currentUser.GetTenant() != "marketintelligence")
        {
            throw new UnauthorizedAccessException();
        }

        var tenantId = currentUser.GetTenant();
        var userId = currentUser.GetUserId().ToString();

        return await dbContext.UserPaymentRequests
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.UserId == userId &&
                x.Status == UserPaymentStatus.Pending)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new MyPendingPaymentRequestResponse(
                x.Id,
                x.PlanKey,
                x.DurationMonths,
                x.OriginalPrice,
                x.DiscountPercent,
                x.Amount,
                x.Currency,
                x.ReceiptFileId != null,
                x.CreatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
