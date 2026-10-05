using FSH.Framework.Core.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.MarketIntelligence.Data.Views;

public sealed class DisclosureStats : IGlobalEntity
{
    public string PersianDate { get; set; } = default!;
    public long DailyCount { get; set; }
    public long MonthlyCount { get; set; }
    public long YearlyCount { get; set; }
    public long TotalCount { get; set; }
    public string? LatestDisclosure { get; set; }
}
