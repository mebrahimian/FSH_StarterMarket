

using FSH.Modules.MarketIntelligence.Service.Codal;


namespace FSH.Modules.MarketIntelligence.Services.Codal;


public sealed class CodalCollectorService : ICodalCollectorService
{
    private readonly ICodalClient _codalClient;
    private readonly ICodalCollectorState _state;

    public CodalCollectorService(
    ICodalClient codalClient,
    ICodalCollectorState state)
    {
        _codalClient = codalClient;
        _state = state;
    }
    
    public async Task CollectAsync(
    CancellationToken cancellationToken = default)
    {
        var lastSuccess =
            await _state.GetLastSuccessfulPublishDateTimeAsync(
                cancellationToken);

        lastSuccess ??= DateTime.Now.AddYears(-1);

        var runStarted =
            await _state.StartRunAsync(
                cancellationToken);

        Console.WriteLine($"Collect from : {lastSuccess}");
        Console.WriteLine($"Run started  : {runStarted}");

        var pageNumber = 1;
        var stop = false;

        while (!stop)
        {
            var result = await _codalClient.SearchAsync(
                new()
                {
                    PageNumber = pageNumber
                },
                cancellationToken);

            Console.WriteLine(
                $"Reading page {pageNumber}/{result.TotalPages}");

            foreach (var letter in result.Letters)
            {
                Console.WriteLine(
                    $"{letter.PublishDateTime}  {letter.Symbol}");

                // این قسمت را بعداً تکمیل می‌کنیم
            }

            if (pageNumber >= result.TotalPages)
                break;

            pageNumber++;
        }

        await _state.CompleteRunAsync(
            runStarted,
            cancellationToken);
    }
}
