using FluentValidation;
using FSH.Modules.MarketIntelligence.Contracts.v1.CodalIncrementalSchedule;

namespace FSH.Modules.MarketIntelligence.Features.v1.CodalIncrementalSchedule;

public sealed class UpdateCodalIncrementalScheduleCommandValidator
    : AbstractValidator<UpdateCodalIncrementalScheduleCommand>
{
    public UpdateCodalIncrementalScheduleCommandValidator()
    {
        RuleFor(x => x.StartHour).InclusiveBetween(0, 23);
        RuleFor(x => x.MorningEndHour).InclusiveBetween(0, 23);
        RuleFor(x => x.EndHour).InclusiveBetween(0, 23);

        RuleFor(x => x.MorningEndHour).GreaterThan(x => x.StartHour);
        RuleFor(x => x.EndHour).GreaterThan(x => x.MorningEndHour);

        RuleFor(x => x.BusyPeriodEndDay).InclusiveBetween(1, 31);

        RuleFor(x => x.BusyMorningMinutes).Must(BeValidInterval);
        RuleFor(x => x.BusyAfternoonMinutes).Must(BeValidInterval);
        RuleFor(x => x.NormalMorningMinutes).Must(BeValidInterval);
        RuleFor(x => x.NormalAfternoonMinutes).Must(BeValidInterval);

        RuleFor(x => x.ThursdayBusyMorningMinutes).Must(BeValidInterval);
        RuleFor(x => x.ThursdayBusyAfternoonMinutes).Must(BeValidInterval);
        RuleFor(x => x.ThursdayNormalMorningMinutes).Must(BeValidInterval);
        RuleFor(x => x.ThursdayNormalAfternoonMinutes).Must(BeValidInterval);

        RuleFor(x => x.FridayMinutes).Must(BeValidInterval);
    }

    private static bool BeValidInterval(int value)
    {
        return value is 5 or 10 or 15 or 30 or 60;
    }
}