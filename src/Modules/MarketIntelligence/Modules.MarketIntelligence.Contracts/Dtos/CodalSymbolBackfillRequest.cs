namespace FSH.Modules.MarketIntelligence.Contracts.Dtos;

public sealed class CodalSymbolBackfillRequest
{
    public required string Symbol
    {
        get;
        init;
    }

    public required string FromDate
    {
        get;
        init;
    }

    public required string ToDate
    {
        get;
        init;
    }
}