using FSH.Modules.MarketIntelligence.Contracts.Dtos;

namespace FSH.Modules.MarketIntelligence.Services.Codal.Interfaces;

public interface ICodalClient
{
    Task<CodalSearchResponse> SearchAsync(
        CodalSearchRequest request,
        CancellationToken cancellationToken = default);
}