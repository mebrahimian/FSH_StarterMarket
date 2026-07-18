namespace FSH.Modules.MarketIntelligence.Services.Codal;

public interface ICodalCollectorState
{
    Task<DateTime?> GetLastSuccessfulPublishDateTimeAsync(
        CancellationToken cancellationToken = default);

    Task<DateTime> StartRunAsync(
        CancellationToken cancellationToken = default);

    Task CompleteRunAsync(
        DateTime successfulPublishDateTime,
        CancellationToken cancellationToken = default);
}