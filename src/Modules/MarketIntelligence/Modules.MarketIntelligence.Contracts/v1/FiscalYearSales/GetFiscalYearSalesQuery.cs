using FSH.Modules.MarketIntelligence.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.MarketIntelligence.Contracts.v1.FiscalYearSales;

public sealed record GetFiscalYearSalesQuery(
    string Symbol,
    string Title)
    : IQuery<FiscalYearSalesDto>;