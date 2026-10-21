using FSH.Framework.Core.Context;
using FSH.Modules.Files.Contracts;
using FSH.Modules.MarketIntelligence.Data;
using Microsoft.EntityFrameworkCore;
using FSH.Modules.MarketIntelligence.Domain;
using FSH.Modules.MarketIntelligence.Contracts.Authorization;
using FSH.Modules.Identity.Contracts.Services;


namespace Modules.MarketIntelligence.Authorization;

public sealed class UserPaymentRequestFileAccessPolicy(
    MarketIntelligenceDbContext dbContext,
    ICurrentUser currentUser,
    IUserPermissionService permissions)
    : IFileAccessPolicy
{
    public string OwnerType => "UserPaymentRequest";

    public async Task<bool> CanAttachAsync(
        Guid? ownerId,
        string currentUserId,
        CancellationToken cancellationToken)
    {
        if (ownerId is null ||
            string.IsNullOrWhiteSpace(currentUserId))
            return false;

        var tenantId = currentUser.GetTenant();

        return await dbContext.UserPaymentRequests
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == ownerId.Value &&
                x.TenantId == tenantId &&
                x.UserId == currentUserId &&
                x.Status == UserPaymentStatus.Pending,
                cancellationToken);
    }

    public async Task<bool> CanReadAsync(
    FileAccessContext context,
    string currentUserId,
    CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.OwnerType != OwnerType ||
            context.OwnerId is null ||
            string.IsNullOrWhiteSpace(currentUserId) ||
            currentUser.GetTenant() != "marketintelligence")
        {
            return false;
        }

        var payment = await dbContext.UserPaymentRequests
            .AsNoTracking()
            .Where(x =>
                x.Id == context.OwnerId.Value &&
                x.TenantId == currentUser.GetTenant())
            .Select(x => new
            {
                x.UserId,
                x.Status,
                x.ReceiptFileId
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (payment is null)
            return false;

        // صاحب پرداخت می‌تواند فایل متصل‌شده خود را بخواند.
        // پیش از اتصال نیز می‌تواند فایل متعلق به همان درخواست را بخواند.
        if (payment.UserId == currentUserId)
        {
            return payment.ReceiptFileId == context.FileAssetId ||
                   (payment.Status == UserPaymentStatus.Pending &&
                    payment.ReceiptFileId is null &&
                    context.CreatedByUserId == currentUserId);
        }

        // مدیر فقط رسیدی را می‌بیند که رسماً به پرداخت متصل شده باشد.
        if (payment.ReceiptFileId != context.FileAssetId)
            return false;

        return await permissions.HasPermissionAsync(
            currentUserId,
            MarketIntelligencePermissions.Payments.Review,
            cancellationToken);
    }

    public Task<bool> CanDeleteAsync(
        FileAccessContext context,
        string currentUserId,
        CancellationToken cancellationToken)
        => Task.FromResult(false);

    public Task<bool> CanChangeVisibilityAsync(
        FileAccessContext context,
        string currentUserId,
        CancellationToken cancellationToken)
        => Task.FromResult(false);
}