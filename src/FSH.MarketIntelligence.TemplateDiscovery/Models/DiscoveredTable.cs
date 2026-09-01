using System;
using System.Collections.Generic;
using System.Text;

namespace FSH.MarketIntelligence.TemplateDiscovery.Models;

internal sealed class DiscoveredTable
{
    public int MetaTableId { get; set; }

    public int Code { get; set; }

    public string? TitleFa { get; set; }

    public string? TitleEn { get; set; }

    public string? AliasName { get; set; }

    public string? VersionNo { get; set; }

    public List<string> RowLabels { get; set; } = [];
}
