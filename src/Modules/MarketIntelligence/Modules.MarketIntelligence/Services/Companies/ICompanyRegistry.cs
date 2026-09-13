namespace FSH.Modules.MarketIntelligence.Services.Companies;

public interface ICompanyRegistry
{
    Task<CompanyIdentity?> FindBySymbolAsync(
        string symbol,
        CancellationToken cancellationToken = default);

    Task<CompanyIdentity> GetOrCreateAsync(
        string symbol,
        string companyName,
        CancellationToken cancellationToken = default);
}

public sealed record CompanyIdentity(
    int CompanyId,
    string Symbol,
    string CompanyName);