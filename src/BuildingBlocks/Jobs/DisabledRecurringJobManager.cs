using Hangfire;
using Hangfire.Common;

namespace FSH.Framework.Jobs;

public sealed class DisabledRecurringJobManager : IRecurringJobManager
{
    public void AddOrUpdate(
        string recurringJobId,
        Job job,
        string cronExpression,
        RecurringJobOptions options)
    {
    }

    public void Trigger(string recurringJobId)
    {
    }

    public void RemoveIfExists(string recurringJobId)
    {
    }
}