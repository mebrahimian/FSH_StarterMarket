using System.Data;
using FSH.Framework.Core.Context;
using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;
using FSH.Framework.Core.Exceptions;

namespace FSH.Modules.MarketIntelligence.Features.v1.UserSubscriptions;

public sealed class SelectSubscriptionSymbolCommandHandler(
    MarketIntelligenceDbContext dbContext,
    ICurrentUser currentUser)
    : ICommandHandler<SelectSubscriptionSymbolCommand, int>
{
    public async ValueTask<int> Handle(
        SelectSubscriptionSymbolCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            command.CompanyId);

        if (!Enum.IsDefined(command.Purpose))
            throw new ArgumentOutOfRangeException(nameof(command),
              command.Purpose,
              "Invalid subscription symbol purpose.");

        string userId = currentUser.GetUserId().ToString();

        string tenantId = currentUser.GetTenant()
            ?? throw new InvalidOperationException(
                "Current tenant is required.");

        DateTime now = DateTime.UtcNow;

        bool isValuation =
            command.Purpose ==
            SubscriptionSymbolPurpose.PortfolioValuation;

        string planKey = await dbContext.UserSubscriptions
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.UserId == userId &&
                x.StartUtc <= now &&
                (x.EndUtc == null || x.EndUtc > now))
            .OrderByDescending(x => x.StartUtc)
            .Select(x => x.PlanKey)
            .FirstOrDefaultAsync(cancellationToken)
            ?? "free";

        var limits = await dbContext.UserPlanLimits
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.PlanKey == planKey,
                cancellationToken)
            ?? throw new InvalidOperationException(
                $"Plan limits not found: {planKey}");

        int limit = isValuation
            ? limits.MaxPortfolioValuationSymbols
            : limits.MaxSelectedSymbols;

        if (isValuation && planKey == "free")
            throw new InvalidOperationException(
                "Free valuation symbols are selected by the system.");

        if (limit == -1)
            throw new InvalidOperationException(
                "Unlimited access does not require symbol selection.");

        if (limit == 0)
            throw new InvalidOperationException(
                "Symbol selection is not available for this plan.");

        bool companyExists = await dbContext.CompanyMaster
            .AsNoTracking()
            .AnyAsync(
                x => x.CompanyId == command.CompanyId,
                cancellationToken);

        if (!companyExists)
            throw new InvalidOperationException(
                "Company not found.");

        if (isValuation)
        {
            bool hasPortfolio = await dbContext
                .InvestmentPortfolioPositions
                .AsNoTracking()
                .AnyAsync(
                    x => x.ParentCompanyId == command.CompanyId,
                    cancellationToken);

            if (!hasPortfolio)
                throw new InvalidOperationException(
                    "Company has no portfolio valuation data.");
        }

        var strategy = dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction =
                await dbContext.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

            bool alreadySelected = isValuation
                ? await dbContext.UserSelectedPortfolioValuationSymbols
                    .AnyAsync(
                        x => x.TenantId == tenantId &&
                             x.UserId == userId &&
                             x.CompanyId == command.CompanyId,
                        cancellationToken)
                : await dbContext.UserSelectedSymbols
                    .AnyAsync(
                        x => x.TenantId == tenantId &&
                             x.UserId == userId &&
                             x.CompanyId == command.CompanyId,
                        cancellationToken);

            if (alreadySelected)
                throw new ConflictException("Symbol already selected.");

            int selectedCount = isValuation
                ? await dbContext.UserSelectedPortfolioValuationSymbols
                    .CountAsync(
                        x => x.TenantId == tenantId &&
                             x.UserId == userId,
                        cancellationToken)
                : await dbContext.UserSelectedSymbols
                    .CountAsync(
                        x => x.TenantId == tenantId &&
                             x.UserId == userId,
                        cancellationToken);

            if (selectedCount >= limit)
                throw new ConflictException(
                    "Symbol selection limit reached.");

            if (isValuation)
            {
                dbContext.UserSelectedPortfolioValuationSymbols.Add(
                    UserSelectedPortfolioValuationSymbol.Create(
                        tenantId,
                        userId,
                        command.CompanyId));
            }
            else
            {
                dbContext.UserSelectedSymbols.Add(
                    UserSelectedSymbol.Create(
                        tenantId,
                        userId,
                        command.CompanyId,
                        now));
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return command.CompanyId;
        });
    }
}