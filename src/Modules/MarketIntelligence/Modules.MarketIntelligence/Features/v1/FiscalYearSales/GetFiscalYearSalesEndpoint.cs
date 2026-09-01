using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.MarketIntelligence.Contracts.Authorization;
using FSH.Modules.MarketIntelligence.Contracts.v1.FiscalYearSales;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.MarketIntelligence.Features.v1.FiscalYearSales;

public static class GetFiscalYearSalesEndpoint
{
    internal static RouteHandlerBuilder MapGetFiscalYearSalesEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet(
                "/fiscal-year-sales",
               (
                 string symbol,
                 string title,
                 string? yearEndDate,
                 IMediator mediator,
                 CancellationToken cancellationToken) =>
                 mediator.Send(
                     new GetFiscalYearSalesQuery(
                        Symbol: symbol,
                        Title: title,
                        YearEndDate: yearEndDate),
                        cancellationToken))
            .WithName("GetFiscalYearSales")
            .WithSummary(
                "Gets fiscal year sales for a symbol based on the report title")
            .RequirePermission(
                MarketIntelligencePermissions.Disclosures.View);
    }
}