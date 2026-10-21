using System.Data;
using FSH.Framework.Core.Context;
using FSH.Modules.MarketIntelligence.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Modules.MarketIntelligence.Features.v1.UserSubscriptions;

public sealed class CancelUserPaymentRequestCommandHandler(
    MarketIntelligenceDbContext dbContext,
    IServiceScopeFactory scopeFactory,
    ICurrentUser currentUser)
    : ICommandHandler<CancelUserPaymentRequestCommand>
{
    public async ValueTask<Unit> Handle(
        CancelUserPaymentRequestCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!currentUser.IsAuthenticated() ||
            currentUser.GetTenant() != "marketintelligence")
        {
            throw new UnauthorizedAccessException();
        }

        var tenantId = currentUser.GetTenant();
        var userId = currentUser.GetUserId().ToString();

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
                         x.TenantId == tenantId &&
                         x.UserId == userId,
                    cancellationToken);

            if (payment is null)
            {
                throw new InvalidOperationException(
                    "Payment request not found.");
            }

            payment.Cancel(command.ConfirmedNoPayment);

            await retryDb.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        });

        return Unit.Value;
    }
}