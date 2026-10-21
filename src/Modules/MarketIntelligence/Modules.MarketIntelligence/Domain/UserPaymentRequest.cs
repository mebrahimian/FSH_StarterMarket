
using FSH.Framework.Core.Domain;
using FSH.Modules.MarketIntelligence.Domain.Enums;

namespace FSH.Modules.MarketIntelligence.Domain;

public enum UserPaymentStatus 
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Cancelled = 3

}

public sealed class UserPaymentRequest : BaseEntity<Guid>
{
    private UserPaymentRequest()
    {
    }

    public string TenantId { get; private set; } = default!;
    public string UserId { get; private set; } = default!;
    public string PlanKey { get; private set; } = default!;
    public int DurationMonths { get; private set; }

    public decimal DiscountPercent { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = default!;

    public Guid? ReceiptFileId { get; private set; }
    public string? BankReferenceNumber { get; private set; }

    public UserPaymentStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public string? ReviewedByUserId { get; private set; }
    public DateTime? ReviewedAtUtc { get; private set; }
    public string? ReviewNote { get; private set; }
    public decimal OriginalPrice { get; private set; }

    public decimal RemainingCredit { get; private set; }

    public decimal UpgradeFee { get; private set; }

    public decimal AppliedCredit { get; private set; }
    public UserPaymentRequestType RequestType { get; private set; }

    public static UserPaymentRequest Create(
     string tenantId,
     string userId,
     string planKey,
     int durationMonths,
     decimal discountPercent,
     decimal originalPrice,
     decimal remainingCredit,
     decimal upgradeFee,
     decimal appliedCredit,
     decimal amount,
     string currency,
     UserPaymentRequestType requestType = UserPaymentRequestType.Purchase)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(planKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(originalPrice);
        ArgumentOutOfRangeException.ThrowIfNegative(remainingCredit);
        ArgumentOutOfRangeException.ThrowIfNegative(upgradeFee);
        ArgumentOutOfRangeException.ThrowIfNegative(appliedCredit);
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        if (durationMonths is not (1 or 3 or 6 or 12))
        {
            throw new ArgumentOutOfRangeException(
                nameof(durationMonths),
                "Duration must be 1, 3, 6, or 12 months.");
        }

        if (discountPercent is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(discountPercent),
                "Discount must be between 0 and 100.");
        }        

        if (upgradeFee < 0 || upgradeFee > remainingCredit)
            throw new ArgumentOutOfRangeException(nameof(upgradeFee));
        
        if (appliedCredit != remainingCredit - upgradeFee)
            throw new ArgumentException(
                "Applied credit must equal remaining credit minus upgrade fee.");

        var expectedAmount = Math.Max(0m, originalPrice - appliedCredit);
        if (amount != expectedAmount)
            throw new ArgumentException(
                "Amount must equal the remaining payable amount.");
        if (!Enum.IsDefined(requestType))
            throw new ArgumentOutOfRangeException(nameof(requestType));
        return new UserPaymentRequest
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId.Trim(),
            UserId = userId.Trim(),
#pragma warning disable CA1308
            PlanKey = planKey.Trim().ToLowerInvariant(),
#pragma warning restore CA1308
            DurationMonths = durationMonths,
            DiscountPercent = discountPercent,

            OriginalPrice = originalPrice,
            RemainingCredit = remainingCredit,
            UpgradeFee = upgradeFee,
            AppliedCredit = appliedCredit,
            Amount = amount,

            Currency = currency.Trim().ToUpperInvariant(),
            Status = UserPaymentStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow,
            RequestType = requestType
        };
    }

    public void AttachReceipt(Guid receiptFileId, string? bankReferenceNumber)
    {
        if (Status != UserPaymentStatus.Pending)
            throw new InvalidOperationException("Payment request is not pending.");

        if (receiptFileId == Guid.Empty)
            throw new ArgumentException("Receipt file is required.", nameof(receiptFileId));

        ReceiptFileId = receiptFileId;
        BankReferenceNumber = bankReferenceNumber?.Trim();
    }

    public void Approve(string reviewedByUserId, DateTime reviewedAtUtc, decimal? paidAmount = null)
    {
        var zeroCashUpgrade = RequestType == UserPaymentRequestType.Upgrade && paidAmount == 0m;

        if (Status != UserPaymentStatus.Pending ||
            (ReceiptFileId is null && !zeroCashUpgrade))
            throw new InvalidOperationException(
                "Payment request cannot be approved.");

        ArgumentException.ThrowIfNullOrWhiteSpace(reviewedByUserId);

        if (paidAmount is < 0m)
            throw new ArgumentOutOfRangeException(nameof(paidAmount));

        Amount = paidAmount ?? Amount;

        Status = UserPaymentStatus.Approved;
        ReviewedByUserId = reviewedByUserId;
        ReviewedAtUtc = reviewedAtUtc.ToUniversalTime();
    }

    public void Reject(string reviewedByUserId, DateTime reviewedAtUtc, string? reason)
    {
        if (Status != UserPaymentStatus.Pending)
            throw new InvalidOperationException("Payment request is not pending.");

        ArgumentException.ThrowIfNullOrWhiteSpace(reviewedByUserId);

        Status = UserPaymentStatus.Rejected;
        ReviewedByUserId = reviewedByUserId;
        ReviewedAtUtc = reviewedAtUtc.ToUniversalTime();
        ReviewNote = reason;
    }
    public void RecalculateUpgradeCredit(decimal remainingCredit)
    {
        if (Status != UserPaymentStatus.Pending ||
            RequestType != UserPaymentRequestType.Upgrade)
        {
            throw new InvalidOperationException(
                "Only pending upgrade requests can be recalculated.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(remainingCredit);

        RemainingCredit = decimal.Round(
            remainingCredit,
            0,
            MidpointRounding.AwayFromZero);

        UpgradeFee = decimal.Round(
            RemainingCredit * 0.25m,
            0,
            MidpointRounding.AwayFromZero);

        AppliedCredit = RemainingCredit - UpgradeFee;

        Amount = Math.Max(0m, OriginalPrice - AppliedCredit);
    }
    public decimal CalculateRemainingCredit(
    DateTime subscriptionStartUtc,
    DateTime subscriptionEndUtc,
    DateTime nowUtc)
    {
        if (Status != UserPaymentStatus.Approved)
            throw new InvalidOperationException(
                "Source payment must be approved.");

        var totalTicks =
            (subscriptionEndUtc - subscriptionStartUtc).Ticks;

        var remainingTicks =
            (subscriptionEndUtc - nowUtc).Ticks;

        if (totalTicks <= 0 ||
            nowUtc < subscriptionStartUtc ||
            remainingTicks <= 0)
        {
            throw new InvalidOperationException(
                "No remaining subscription time.");
        }

        return decimal.Round(
            (Amount + AppliedCredit) * remainingTicks / totalTicks,
            0,
            MidpointRounding.AwayFromZero);
    }
    public void Cancel(bool confirmedNoPayment)
    {
        if (Status != UserPaymentStatus.Pending)
            throw new InvalidOperationException(
                "Only pending payment requests can be cancelled.");

        if (ReceiptFileId is not null)
            throw new InvalidOperationException(
                "Payment requests with receipts cannot be cancelled.");

        if (Amount > 0m && !confirmedNoPayment)
            throw new InvalidOperationException(
                "User must confirm that no payment was made.");

        Status = UserPaymentStatus.Cancelled;
    }
}
