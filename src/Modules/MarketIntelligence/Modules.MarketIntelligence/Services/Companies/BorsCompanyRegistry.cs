using FSH.Framework.Shared.Utilities;
using FSH.Modules.MarketIntelligence.Data;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MarketIntelligence.Services.Companies;

public sealed class BorsCompanyRegistry(
    MarketIntelligenceDbContext dbContext)
    : ICompanyRegistry
{
    public async Task<CompanyIdentity?> FindBySymbolAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        string normalizedSymbol =
            FSort.Normalize(symbol);

        var companies =
            await dbContext.CompanyMaster
                .AsNoTracking()
                .Where(x =>
                    x.IsListed &&
                    x.Symbol != null)
                .Select(x => new
                {
                    x.CompanyId,
                    x.Symbol,
                    x.CompanyName
                })
                .ToListAsync(cancellationToken);

        var matches =
            companies
                .Where(x =>
                    FSort.Normalize(x.Symbol!) ==
                    normalizedSymbol)
                .ToList();

        if (matches.Count == 0)
        {
            return null;
        }

        if (matches.Count > 1)
        {
            throw new InvalidOperationException(
                $"More than one company was found for symbol '{symbol}'.");
        }

        var company =
            matches[0];

        return new CompanyIdentity(
            company.CompanyId,
            company.Symbol!,
            company.CompanyName);
    }

    public async Task<CompanyIdentity> GetOrCreateAsync(
        string symbol,
        string companyName,
        CancellationToken cancellationToken = default)
    {
        CompanyIdentity? existing =
            await FindBySymbolAsync(
                symbol,
                cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        throw new InvalidOperationException(
            $"Company '{symbol}' was not found in the company registry. " +
            "Automatic creation is not enabled yet.");
    }
}