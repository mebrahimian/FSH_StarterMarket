using System;
using System.Collections.Generic;
using System.Text;

namespace FSH.MarketIntelligence.TemplateDiscovery.Models;

internal sealed class DiscoveredTemplate
{
    public string? TitleFa { get; set; }

    public string? TitleEn { get; set; }

    public int Type { get; set; }

    public int Period { get; set; }

    public string? PeriodEndToDate { get; set; }

    public string? YearEndToDate { get; set; }

    public List<DiscoveredTable> Tables { get; set; } = [];
}
