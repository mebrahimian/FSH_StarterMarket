using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Domain;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MarketIntelligence.Services.Codal.Portfolio;

public sealed class PortfolioHoldingAssetIdentityResolver(
    MarketIntelligenceDbContext dbContext)
{
    public async Task<PortfolioHoldingAssetIdentity> ResolveAsync(
        PortfolioHoldingAsset asset,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(asset);

        if (asset.ListedCompanyId.HasValue &&
            !asset.UnlistedCompanyId.HasValue)
        {
            return new PortfolioHoldingAssetIdentity(
                asset.ListedCompanyId.Value,
                true);
        }

        if (!asset.ListedCompanyId.HasValue &&
            asset.UnlistedCompanyId.HasValue)
        {
            return new PortfolioHoldingAssetIdentity(
                asset.UnlistedCompanyId.Value,
                false);
        }

        if (!asset.ListedCompanyId.HasValue ||
            !asset.UnlistedCompanyId.HasValue)
        {
            throw new InvalidOperationException(
                $"HoldingAsset {asset.Id} has no company identity.");
        }

        var listedCompany =
            await dbContext.CompanyMaster
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x =>
                        x.CompanyId ==
                        asset.ListedCompanyId.Value,
                    cancellationToken)
                .ConfigureAwait(false);

        if (listedCompany is null ||
            string.IsNullOrWhiteSpace(listedCompany.Symbol))
        {
            return new PortfolioHoldingAssetIdentity(
                asset.UnlistedCompanyId.Value,
                false);
        }

        int? instrumentId =
            await dbContext.TsetmcInstruments
                .AsNoTracking()
                .Where(x =>
                    x.Symbol == listedCompany.Symbol)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

        if (!instrumentId.HasValue)
        {
            return new PortfolioHoldingAssetIdentity(
                asset.UnlistedCompanyId.Value,
                false);
        }

        DateOnly cutoff =
            DateOnly.FromDateTime(
                DateTime.UtcNow.Date.AddYears(-1));

        bool hasRecentRealTrade =
            await dbContext.DailyPrices
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.InstrumentId == instrumentId.Value &&
                        x.TradeDate >= cutoff &&
                        x.Volume > 0,
                    cancellationToken)
                .ConfigureAwait(false);

        return hasRecentRealTrade
            ? new PortfolioHoldingAssetIdentity(
                asset.ListedCompanyId.Value,
                true)
            : new PortfolioHoldingAssetIdentity(
                asset.UnlistedCompanyId.Value,
                false);
    }
}

public sealed record PortfolioHoldingAssetIdentity(
    int CompanyId,
    bool IsListed);