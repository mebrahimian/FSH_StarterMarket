using System.Text.Json.Serialization;

namespace FSH.Modules.MarketIntelligence.Services.Codal.Contracts;

public sealed class CodalIndustryDto
{
    public int Id { get; set; }

    public string? Name { get; set; }
}
public sealed class CodalCompanyDto
{
    [JsonPropertyName("sy")]
    public string? Symbol { get; set; }

    [JsonPropertyName("n")]
    public string? Name { get; set; }

    [JsonPropertyName("i")]
    public string? Isic { get; set; }

    [JsonPropertyName("t")]
    public int? Type { get; set; }

    [JsonPropertyName("st")]
    public int? State { get; set; }

    [JsonPropertyName("IG")]
    public int? IndustryGroupCode { get; set; }

    [JsonPropertyName("RT")]
    public int? ReportingType { get; set; }
}