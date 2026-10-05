using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Domain;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MarketIntelligence.Services.Jobs;

public sealed class BackgroundJobStatusService(
    MarketIntelligenceDbContext dbContext)
{
    public async Task StartedAsync(
        string jobCode,
        string jobName,
        CancellationToken cancellationToken)
    {
        BackgroundJobStatus? status =
            await dbContext.BackgroundJobStatuses
                .SingleOrDefaultAsync(
                    x => x.JobCode == jobCode,
                    cancellationToken)
                .ConfigureAwait(false);

        if (status is null)
        {
            status = new BackgroundJobStatus(
                jobCode,
                jobName);

            dbContext.BackgroundJobStatuses.Add(status);
        }

        status.MarkStarted();

        await dbContext
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task SucceededAsync(
        string jobCode,
        int? processed,
        int? inserted,
        int? updated,
        CancellationToken cancellationToken)
    {
        BackgroundJobStatus? status =
            await dbContext.BackgroundJobStatuses
                .SingleOrDefaultAsync(
                    x => x.JobCode == jobCode,
                    cancellationToken)
                .ConfigureAwait(false);

        if (status is null)
        {
            return;
        }

        status.MarkSucceeded(
            processed,
            inserted,
            updated);

        await dbContext
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task FailedAsync(
        string jobCode,
        Exception exception,
        CancellationToken cancellationToken)
    {
        BackgroundJobStatus? status =
            await dbContext.BackgroundJobStatuses
                .SingleOrDefaultAsync(
                    x => x.JobCode == jobCode,
                    cancellationToken)
                .ConfigureAwait(false);

        if (status is null)
        {
            return;
        }

        status.MarkFailed(exception);

        await dbContext
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }
    public async Task ExecuteAsync(string jobCode, string jobName, Func<Task> action, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(jobName);
        ArgumentNullException.ThrowIfNull(action);

        await StartedAsync(
            jobCode,
            jobName,
            cancellationToken)
            .ConfigureAwait(false);

        try
        {
            await action()
                .ConfigureAwait(false);

            await SucceededAsync(
                jobCode,
                null,
                null,
                null,
                cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            await FailedAsync(
                jobCode,
                exception,
                cancellationToken)
                .ConfigureAwait(false);

            throw;
        }
    }
}