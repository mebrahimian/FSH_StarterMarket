namespace FSH.Modules.MarketIntelligence.Contracts.Dtos;

public sealed record AssetIdentityDto(
    int CompanyId,
    int? PortfolioHoldingAssetId,
    int? TsetmcInstrumentId,
    string Symbol,
    string? Name,
    string? InsCode,
    string? Isin,
    string? Isic,
    string? YVal);