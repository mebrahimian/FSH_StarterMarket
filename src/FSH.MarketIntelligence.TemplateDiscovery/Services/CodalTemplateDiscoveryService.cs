using FSH.MarketIntelligence.TemplateDiscovery.Models;
using System.Text.Json;

namespace FSH.MarketIntelligence.TemplateDiscovery.Services;

public sealed class CodalTemplateDiscoveryService
{
    public DiscoveredTemplate Extract(string json)
    {
        using var doc = JsonDocument.Parse(json);

        var root = doc.RootElement;

        var template = new DiscoveredTemplate
        {
            TitleFa = root.TryGetProperty("title_Fa", out var titleFa)
                ? titleFa.GetString()
                : null,

            TitleEn = root.TryGetProperty("title_En", out var titleEn)
                ? titleEn.GetString()
                : null,

            Type = root.TryGetProperty("type", out var type)
                ? type.GetInt32()
                : 0,

            Period = root.TryGetProperty("period", out var period)
                ? period.GetInt32()
                : 0,

            PeriodEndToDate = root.TryGetProperty("periodEndToDate", out var pe)
                ? pe.GetString()
                : null,

            YearEndToDate = root.TryGetProperty("yearEndToDate", out var ye)
                ? ye.GetString()
                : null
        };

        return template;
    }
}