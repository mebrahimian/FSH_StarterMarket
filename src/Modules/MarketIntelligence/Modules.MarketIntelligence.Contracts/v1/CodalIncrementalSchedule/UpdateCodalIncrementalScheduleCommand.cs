using Mediator;

namespace FSH.Modules.MarketIntelligence.Contracts.v1.CodalIncrementalSchedule;

public sealed record UpdateCodalIncrementalScheduleCommand(
    int StartHour,
    int MorningEndHour,
    int EndHour,
    int BusyPeriodEndDay,
    int BusyMorningMinutes,
    int BusyAfternoonMinutes,
    int NormalMorningMinutes,
    int NormalAfternoonMinutes,
    int ThursdayBusyMorningMinutes,
    int ThursdayBusyAfternoonMinutes,
    int ThursdayNormalMorningMinutes,
    int ThursdayNormalAfternoonMinutes,
    int FridayMinutes)
    : ICommand<CodalIncrementalScheduleDto>;