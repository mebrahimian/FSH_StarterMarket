using FSH.Modules.MarketIntelligence.Contracts.v1.PortfolioMatching;
using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MarketIntelligence.Features.v1.PortfolioMatching;

public sealed class MatchPortfolioCompanyCommandHandler(
    MarketIntelligenceDbContext dbContext)
    : ICommandHandler<MatchPortfolioCompanyCommand, int>
{
    public async ValueTask<int> Handle(
        MatchPortfolioCompanyCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.RawCompanyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.FSortName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(command.CompanyId);

        string rawCompanyName = command.RawCompanyName.Trim();
        string fSortName = command.FSortName.Trim();

        var strategy =
            dbContext.Database.CreateExecutionStrategy();

        int updatedCount =
            await strategy.ExecuteAsync(
                async () =>
                {
                    await using var transaction =
                        await dbContext.Database
                            .BeginTransactionAsync(cancellationToken)
                            .ConfigureAwait(false);

                    // وضعیت بورسی/غیربورسی از CompanyMaster مقصد می‌آید،
                    // نه از شیت کدال.
                    var targetCompany =
                        await dbContext.CompanyMaster
                            .AsNoTracking()
                            .FirstOrDefaultAsync(
                                company =>
                                    company.CompanyId == command.CompanyId,
                                cancellationToken)
                            .ConfigureAwait(false);

                    if (targetCompany is null)
                    {
                        throw new InvalidOperationException(
                            "The selected target company was not found.");
                    }

                    // تمام Aliasهای همین نام را بررسی می‌کنیم،
                    // بدون توجه به IsListed.
                    List<PortfolioCompanyAlias> existingAliases =
                        await dbContext.PortfolioCompanyAliases
                            .Where(alias =>
                                alias.FSortName == fSortName)
                            .ToListAsync(cancellationToken)
                            .ConfigureAwait(false);

                    // اگر Alias صحیح و فعال از قبل وجود دارد، نگهش می‌داریم.
                    PortfolioCompanyAlias? canonicalAlias =
                        existingAliases.FirstOrDefault(alias =>
                            alias.IsActive &&
                            alias.CompanyId == targetCompany.CompanyId &&
                            alias.IsListed == targetCompany.IsListed);

                    // Aliasهای متناقض همین FSortName حذف می‌شوند.
                    List<PortfolioCompanyAlias> aliasesToRemove =
                        existingAliases
                            .Where(alias =>
                                canonicalAlias is null ||
                                alias.Id != canonicalAlias.Id)
                            .ToList();

                    if (aliasesToRemove.Count > 0)
                    {
                        dbContext.PortfolioCompanyAliases
                            .RemoveRange(aliasesToRemove);

                        await dbContext
                            .SaveChangesAsync(cancellationToken)
                            .ConfigureAwait(false);
                    }

                    // اگر Alias صحیح وجود نداشت، یک Alias canonical می‌سازیم.
                    if (canonicalAlias is null)
                    {
                        dbContext.PortfolioCompanyAliases.Add(
                            new PortfolioCompanyAlias(
                                targetCompany.CompanyId,
                                targetCompany.Symbol,
                                targetCompany.FSortSymbol,
                                rawCompanyName,
                                fSortName,
                                targetCompany.IsListed));

                        await dbContext
                            .SaveChangesAsync(cancellationToken)
                            .ConfigureAwait(false);
                    }

                    // تصمیم دستی کاربر برای تمام رکوردهای همین FSortName
                    // مرجع است؛ حتی اگر قبلاً اشتباه Match شده باشند.
                    int count =
                        await dbContext.InvestmentPortfolioPositions
                            .Where(position =>
                                position.FSortName == fSortName)
                            .ExecuteUpdateAsync(
                                setters => setters
                                    .SetProperty(
                                        position => position.ChildCompanyId,
                                        command.CompanyId)
                                    .SetProperty(
                                        position => position.IsListed,
                                        targetCompany.IsListed),
                                cancellationToken)
                            .ConfigureAwait(false);

                    await transaction
                        .CommitAsync(cancellationToken)
                        .ConfigureAwait(false);

                    return count;
                })
            .ConfigureAwait(false);

        return updatedCount;
    }
}