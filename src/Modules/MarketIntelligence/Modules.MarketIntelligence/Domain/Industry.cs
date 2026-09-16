using FSH.Framework.Core.Domain;
namespace FSH.Modules.MarketIntelligence.Domain;

public sealed class Industry : IGlobalEntity
{
    public string IndustryId { get; set; } = string.Empty;

    public string IndustryName { get; set; } = string.Empty;
}