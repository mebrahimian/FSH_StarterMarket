using FSH.Framework.Core.Domain;

namespace FSH.Modules.MarketIntelligence.Domain;

public sealed class CodalIncrementalScheduleSetting
    : BaseEntity<Guid>, IGlobalEntity
{
    public int StartHour { get; set; } = 8;

    public int MorningEndHour { get; set; } = 13;

    public int EndHour { get; set; } = 19;

    public int BusyPeriodEndDay { get; set; } = 10;

    public int BusyMorningMinutes { get; set; } = 5;

    public int BusyAfternoonMinutes { get; set; } = 15;

    public int NormalMorningMinutes { get; set; } = 15;

    public int NormalAfternoonMinutes { get; set; } = 30;

    public int ThursdayBusyMorningMinutes { get; set; } = 15;

    public int ThursdayBusyAfternoonMinutes { get; set; } = 30;

    public int ThursdayNormalMorningMinutes { get; set; } = 30;

    public int ThursdayNormalAfternoonMinutes { get; set; } = 60;

    public int FridayMinutes { get; set; } = 60;
}