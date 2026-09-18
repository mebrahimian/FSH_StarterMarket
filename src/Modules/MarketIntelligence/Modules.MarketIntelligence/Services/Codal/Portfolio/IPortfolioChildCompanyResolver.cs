namespace FSH.Modules.MarketIntelligence.Services.Codal.Portfolio;

public interface IPortfolioChildCompanyResolver
{
    Task<PortfolioChildCompanyResolution> ResolveAsync(
        string rawCompanyName,
        bool reportedIsListed,
        CancellationToken cancellationToken);
}