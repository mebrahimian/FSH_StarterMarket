
namespace FSH.Modules.MarketIntelligence.Services.Codal;

public sealed class InMemoryCodalCollectorState : ICodalCollectorState
{
    private DateTime? _lastSuccessfulPublishDateTime;

    public Task<DateTime?> GetLastSuccessfulPublishDateTimeAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            _lastSuccessfulPublishDateTime);
    }


    public Task<DateTime> StartRunAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(DateTime.Now);
    }


    public Task CompleteRunAsync(
        DateTime successfulPublishDateTime,
        CancellationToken cancellationToken = default)
    {
        _lastSuccessfulPublishDateTime =
            successfulPublishDateTime;

        return Task.CompletedTask;
    }
}
