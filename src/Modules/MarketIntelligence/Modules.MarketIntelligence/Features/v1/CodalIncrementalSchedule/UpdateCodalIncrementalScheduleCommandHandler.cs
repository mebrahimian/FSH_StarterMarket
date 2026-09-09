using FSH.Modules.MarketIntelligence.Contracts.v1.CodalIncrementalSchedule;
using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MarketIntelligence.Features.v1.CodalIncrementalSchedule;

public sealed class UpdateCodalIncrementalScheduleCommandHandler(
    MarketIntelligenceDbContext dbContext)
    : ICommandHandler<
        UpdateCodalIncrementalScheduleCommand,
        CodalIncrementalScheduleDto>
{
    public async ValueTask<CodalIncrementalScheduleDto> Handle(
        UpdateCodalIncrementalScheduleCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        CodalIncrementalScheduleSetting? setting =
            await dbContext.CodalIncrementalScheduleSettings
                .SingleOrDefaultAsync(cancellationToken);

        if (setting is null)
        {
            setting = new CodalIncrementalScheduleSetting();
            dbContext.CodalIncrementalScheduleSettings.Add(setting);
        }

        setting.StartHour = command.StartHour;
        setting.MorningEndHour = command.MorningEndHour;
        setting.EndHour = command.EndHour;
        setting.BusyPeriodEndDay = command.BusyPeriodEndDay;
        setting.BusyMorningMinutes = command.BusyMorningMinutes;
        setting.BusyAfternoonMinutes = command.BusyAfternoonMinutes;
        setting.NormalMorningMinutes = command.NormalMorningMinutes;
        setting.NormalAfternoonMinutes = command.NormalAfternoonMinutes;
        setting.ThursdayBusyMorningMinutes = command.ThursdayBusyMorningMinutes;
        setting.ThursdayBusyAfternoonMinutes = command.ThursdayBusyAfternoonMinutes;
        setting.ThursdayNormalMorningMinutes = command.ThursdayNormalMorningMinutes;
        setting.ThursdayNormalAfternoonMinutes = command.ThursdayNormalAfternoonMinutes;
        setting.FridayMinutes = command.FridayMinutes;

        await dbContext.SaveChangesAsync(cancellationToken);

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