using FSH.Framework.Jobs.Services;
using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Domain;
using FSH.Modules.MarketIntelligence.Domain.Enums;
using FSH.Modules.MarketIntelligence.Services.Codal.Interfaces;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace FSH.Modules.MarketIntelligence.Services.Codal.Jobs;

public sealed class CodalBackgroundJob(
    ICodalCollectorService collectorService,
    MarketIntelligenceDbContext dbContext,
    IJobService jobService)
{
    private async Task<CodalIncrementalScheduleSetting> GetScheduleSettingAsync(
    CancellationToken cancellationToken)
    {
        CodalIncrementalScheduleSetting? setting =
            await dbContext.CodalIncrementalScheduleSettings
                .SingleOrDefaultAsync(cancellationToken);

        if (setting is not null)
        {
            return setting;
        }

        setting = new CodalIncrementalScheduleSetting();

        dbContext.CodalIncrementalScheduleSettings.Add(setting);

        await dbContext.SaveChangesAsync(cancellationToken);

        return setting;
    }
    public Task RunIncrementalAsync(
    CancellationToken cancellationToken)
    {
        return collectorService
            .CollectIncrementalAsync(
                cancellationToken);
    }

    public async Task RunScheduledIncrementalAsync(
    CancellationToken cancellationToken)
    {
        CodalIncrementalScheduleSetting setting =
    await GetScheduleSettingAsync(cancellationToken);

        if (!ShouldRunIncremental(setting))
        {
            return;
        }

        await collectorService
            .CollectIncrementalAsync(
                cancellationToken);
    }
    
    [AutomaticRetry(Attempts = 0)]
    public Task RunSymbolBackfillAsync(string symbol, string fromDate, string toDate)
    {
        return collectorService
            .CollectSymbolBackfillAsync(symbol, fromDate, toDate, CancellationToken.None);
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task RunParsePendingAsync(
     CancellationToken cancellationToken)
    {
        bool shouldContinue = await collectorService
                               .ParsePendingDisclosuresAsync(cancellationToken);
        if (!shouldContinue)
        {
            return;
        }

        bool hasPending = await dbContext.Disclosures
                .AnyAsync(x => x.SalesParseStatus == DisclosureParseStatus.Pending, cancellationToken);

        if (!hasPending)
        {
            return;
        }

        jobService.Enqueue<CodalBackgroundJob>(job => job.RunParsePendingAsync(CancellationToken.None));
    }
    private static bool ShouldRunIncremental(
    CodalIncrementalScheduleSetting setting)
    {
        TimeZoneInfo tehranTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById(
                "Iran Standard Time");

        DateTime now =
            TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                tehranTimeZone);

        var persianCalendar =
            new PersianCalendar();

        int persianDay = persianCalendar.GetDayOfMonth(now);

        bool isBusyPeriod = persianDay <= setting.BusyPeriodEndDay;

        bool isMorning = now.Hour < setting.MorningEndHour;

        int intervalMinutes;

        if (now.DayOfWeek == DayOfWeek.Friday)
        {
            intervalMinutes = setting.FridayMinutes;
        }
        else if (now.DayOfWeek == DayOfWeek.Thursday)
        {
            if (isBusyPeriod)
            {
                intervalMinutes = isMorning
                    ? setting.ThursdayBusyMorningMinutes
                    : setting.ThursdayBusyAfternoonMinutes;
            }
            else
            {
                intervalMinutes = isMorning
                    ? setting.ThursdayNormalMorningMinutes
                    : setting.ThursdayNormalAfternoonMinutes;
            }
        }
        else
        {
            if (isBusyPeriod)
            {
                intervalMinutes = isMorning
                    ? setting.BusyMorningMinutes
                    : setting.BusyAfternoonMinutes;
            }
            else
            {
                intervalMinutes = isMorning
                    ? setting.NormalMorningMinutes
                    : setting.NormalAfternoonMinutes;
            }
        }

        return now.Minute % intervalMinutes == 0;
    }
    [AutomaticRetry(Attempts = 0)]
    public async Task RunBackfillChunkAsync(
    int startPage,
    int endPage,
    CancellationToken cancellationToken)
    {
        await collectorService.CollectBackfillChunkAsync(
            startPage,
            endPage,
            cancellationToken);

        if (endPage <= 1)
        {
            return;
        }

        int nextStartPage = endPage - 1;
        int nextEndPage = Math.Max(1, nextStartPage - 199);

        jobService.Enqueue<CodalBackgroundJob>(
            job => job.RunBackfillChunkAsync(
                nextStartPage,
                nextEndPage,
                CancellationToken.None));
    }
}