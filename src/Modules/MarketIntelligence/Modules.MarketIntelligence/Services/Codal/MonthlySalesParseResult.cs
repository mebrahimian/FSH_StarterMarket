namespace FSH.Modules.MarketIntelligence.Services.Codal;

public sealed class MonthlySalesParseResult
{
    public decimal MonthlySales { get; set; }

    public decimal YearToDateSales { get; set; }

    public string? PeriodEndToDate { get; set; }

    public string? YearEndToDate { get; set; }
}