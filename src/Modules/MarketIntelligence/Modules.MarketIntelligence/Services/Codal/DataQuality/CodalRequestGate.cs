using Microsoft.Extensions.Logging;
using FSH.Modules.MarketIntelligence.Services.Codal;

namespace FSH.Modules.MarketIntelligence.Services.Codal;

public sealed class CodalRequestGate : IDisposable
{
    private readonly ILogger<CodalRequestGate> _logger;
    private readonly Queue<DateTimeOffset> _requestHistory = new();
    public CodalRequestGate(
    ILogger<CodalRequestGate> logger)
    {
        _logger = logger;
    }
    private static readonly TimeSpan MinimumInterval =
        TimeSpan.FromSeconds(25);

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
            int retryCount = 0;
            while (true)
            {
                DateTimeOffset requestStartedAt = DateTimeOffset.UtcNow;

                _lastRequestStartedAt = requestStartedAt;

                _requestHistory.Enqueue( requestStartedAt);

                DateTimeOffset cutoff = requestStartedAt.AddHours(-1);

                while (
                    _requestHistory.TryPeek(
                        out DateTimeOffset oldest) &&
                    oldest < cutoff)
                {
                    _requestHistory.Dequeue();
                }

                try
                {
                    return await action(cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (HttpRequestException ex)
                    when (
                        ex.StatusCode ==
                        System.Net.HttpStatusCode.TooManyRequests)
                {
                    if (++retryCount > 3)
                    {
                        throw;
                    }
                    DateTimeOffset now = DateTimeOffset.UtcNow;

                    _logger.LogWarning(ex, "Codal 429. Requests: 1m={Requests1m}, 5m={Requests5m}, 15m={Requests15m}, 60m={Requests60m}",
                       _requestHistory.Count(x =>
                                       x >= now.AddMinutes(-1)),
                       _requestHistory.Count(x =>
                                       x >= now.AddMinutes(-5)),
                       _requestHistory.Count(x =>
                                       x >= now.AddMinutes(-15)),
                       _requestHistory.Count(x =>
                                       x >= now.AddHours(-1)));
                    await Task.Delay(
                            TimeSpan.FromMinutes(10),
                            cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (HttpRequestException ex)
                {
                    if (++retryCount > 3)
                    {
                        throw;
                    }
                    _logger.LogWarning(
                        ex,
                        "Codal HTTP connection error. Retrying after 2 minutes.");

                    await Task.Delay(
                        TimeSpan.FromMinutes(2),
                        cancellationToken).ConfigureAwait(false);
                }
            }
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