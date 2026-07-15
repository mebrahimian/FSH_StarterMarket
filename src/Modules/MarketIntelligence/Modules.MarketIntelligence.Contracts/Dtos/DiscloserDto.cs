namespace FSH.Modules.MarketIntelligence.Contracts.Dtos;

public sealed record DisclosureDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? LogoUrl,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTimeOffset? DeletedOnUtc = null,
    string? DeletedBy = null);
