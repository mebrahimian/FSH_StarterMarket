using FSH.Framework.Core.Context;
using FSH.Modules.MarketIntelligence.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MarketIntelligence.Features.v1.UserSubscriptions;

public sealed class GetPaymentRequestsQueryHandler(
    MarketIntelligenceDbContext dbContext,
    ICurrentUser currentUser)
    : IQueryHandler<GetPaymentRequestsQuery, GetPaymentRequestsResponse>
{
    public async ValueTask<GetPaymentRequestsResponse> Handle(
        GetPaymentRequestsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!currentUser.IsAuthenticated() ||
            currentUser.GetTenant() != "marketintelligence")
        {
            throw new UnauthorizedAccessException();
        }

        var pageNumber = Math.Max(1, query.PageNumber);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var tenantId = currentUser.GetTenant();

        var payments = dbContext.UserPaymentRequests
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId);

        if (query.Status.HasValue)
        {
            payments = payments.Where(
                x => x.Status == query.Status.Value);
        }

        var totalCount = await payments.CountAsync(cancellationToken);

        var items = await payments
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new PaymentRequestListItem(
                x.Id,
                x.UserId,
                x.PlanKey,
                x.DurationMonths,
                x.Amount,
                x.Currency,
                x.Status,
                x.ReceiptFileId,
                x.BankReferenceNumber,
                x.CreatedAtUtc,
                x.ReviewedByUserId,
                x.ReviewedAtUtc,
                x.ReviewNote))
            .ToListAsync(cancellationToken);

        return new GetPaymentRequestsResponse(
            items,
            totalCount,
            pageNumber,
            pageSize);
    }
}