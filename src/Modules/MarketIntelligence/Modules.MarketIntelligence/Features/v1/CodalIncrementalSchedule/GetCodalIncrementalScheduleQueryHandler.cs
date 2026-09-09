using FSH.Modules.MarketIntelligence.Contracts.v1.CodalIncrementalSchedule;
using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MarketIntelligence.Features.v1.CodalIncrementalSchedule;

public sealed class GetCodalIncrementalScheduleQueryHandler(
    MarketIntelligenceDbContext dbContext)
    : IQueryHandler<
        GetCodalIncrementalScheduleQuery,
        CodalIncrementalScheduleDto>
{
    public async ValueTask<CodalIncrementalScheduleDto> Handle(
        GetCodalIncrementalScheduleQuery query,
        CancellationToken cancellationToken)
    {
        CodalIncrementalScheduleSetting? setting =
            await dbContext.CodalIncrementalScheduleSettings
                .SingleOrDefaultAsync(cancellationToken);

        setting ??= new CodalIncrementalScheduleSetting();

        return new CodalIncrementalScheduleDto(
            setting.StartHour,
            setting.MorningEndHour,
            setting.EndHour,
            setting.BusyPeriodEndDay,
            setting.BusyMorningMinutes,
            setting.BusyAfternoonMinutes,
            setting.NormalMorningMinutes,
            setting.NormalAfternoonMinutes,
            setting.ThursdayBusyMorningMinutes,
            setting.ThursdayBusyAfternoonMinutes,
            setting.ThursdayNormalMorningMinutes,
            setting.ThursdayNormalAfternoonMinutes,
            setting.FridayMinutes);
    }
}