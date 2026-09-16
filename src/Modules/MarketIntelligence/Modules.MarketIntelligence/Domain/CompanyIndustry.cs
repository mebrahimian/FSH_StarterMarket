using FSH.Framework.Core.Domain;
namespace FSH.Modules.MarketIntelligence.Domain;

public sealed class CompanyIndustry : IGlobalEntity
{
    public int CompanyId { get; set; }

    public string IndustryId { get; set; } = string.Empty;

    public string? IndustryGroupId { get; set; }

    public string? Isic { get; set; }
}

public sealed class CodalCompanyImport : IGlobalEntity
{
    public int Id { get; set; }

    public string Symbol { get; set; } = default!;

    public string? CompanyName { get; set; }

    public string IndustryId { get; set; } = default!;

    public string Isic { get; set; } = default!;

    public string IndustryGroupId { get; set; } = default!;

    public int? ReportingType { get; set; }

    public int? State { get; set; }

    public int? CompanyType { get; set; }
}