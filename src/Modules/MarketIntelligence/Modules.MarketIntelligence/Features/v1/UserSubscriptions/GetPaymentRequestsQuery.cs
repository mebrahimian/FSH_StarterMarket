using FSH.Modules.MarketIntelligence.Domain;
using Mediator;

namespace FSH.Modules.MarketIntelligence.Features.v1.UserSubscriptions;

public sealed record GetPaymentRequestsQuery(
    UserPaymentStatus? Status = null,
    int PageNumber = 1,
    int PageSize = 20
) : IQuery<GetPaymentRequestsResponse>;

public sealed record PaymentRequestListItem(
    Guid Id,
    string UserId,
    string PlanKey,
    int DurationMonths,
    decimal Amount,
    string Currency,
    UserPaymentStatus Status,
    Guid? ReceiptFileId,
    string? BankReferenceNumber,
    DateTime CreatedAtUtc,
    string? ReviewedByUserId,
    DateTime? ReviewedAtUtc,
    string? ReviewNote);

public sealed record GetPaymentRequestsResponse(
    IReadOnlyList<PaymentRequestListItem> Items,
    int TotalCount,
    int PageNumber,
    int PageSize);