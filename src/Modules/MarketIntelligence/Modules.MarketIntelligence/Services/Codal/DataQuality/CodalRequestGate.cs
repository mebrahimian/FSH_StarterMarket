namespace FSH.Modules.MarketIntelligence.Services.Codal;

public sealed class CodalRequestGate : IDisposable
{
    private static readonly TimeSpan MinimumInterval =
        TimeSpan.FromSeconds(15);

    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private DateTimeOffset? _lastRequestStartedAt;

    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);

        await _semaphore
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            if (_lastRequestStartedAt.HasValue)
            {
                TimeSpan elapsed =
                    DateTimeOffset.UtcNow -
                    _lastRequestStartedAt.Value;

                TimeSpan remaining =
                    MinimumInterval - elapsed;

                if (remaining > TimeSpan.Zero)
                {
                    await Task.Delay(
                            remaining,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
            }

            _lastRequestStartedAt =
                DateTimeOffset.UtcNow;

            return await action(cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        _semaphore.Dispose();
    }
}