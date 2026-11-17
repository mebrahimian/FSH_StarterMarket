
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.MarketIntelligence.Contracts.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.MarketIntelligence.Features.v1.RealMoneyFlow;

public static class GetRealMoneyFlowReportEndpoint
{
    internal static RouteHandlerBuilder MapGetRealMoneyFlowReportEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet(
                "/real-money-flow",
                (
                    DateOnly? tradeDate,
                    int? lookbackDays,
                    decimal? minZScore,
                    decimal? minFlowMultiple,
                    decimal? minTradeValueMultiple,
                    int? companyId,
                    int? pageNumber,
                    int? pageSize,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                {
                    var request = new RealMoneyFlowReportRequest(
                        TradeDate: tradeDate,
                        LookbackDays: lookbackDays ?? 20,
                        MinZScore: minZScore ?? 1.5m,
                        MinFlowMultiple: minFlowMultiple ?? 2m,
                        MinTradeValueMultiple: minTradeValueMultiple ?? 1.5m,
                        CompanyId: companyId,
                        PageNumber: pageNumber ?? 1,
                        PageSize: pageSize ?? 20);

                    return mediator.Send(
                        new GetRealMoneyFlowReportQuery(request),
                        cancellationToken);
                })
            .WithName("GetRealMoneyFlowReport")
            .WithSummary(
                "Gets parameterized real money flow signals")
            .RequirePermission(
                MarketIntelligencePermissions.Disclosures.View);
    }
}
