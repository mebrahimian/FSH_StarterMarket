using Asp.Versioning;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Web.Modules;
using FSH.Modules.MarketIntelligence.Contracts.Authorization;
using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Features.v1.Disclosures.SearchDisclosures;
using FSH.Modules.MarketIntelligence.Services.Codal;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using FSH.Modules.MarketIntelligence.Services.Codal.Interfaces;
using FSH.Modules.MarketIntelligence.Services.Codal.Processors;

[assembly: FshModule(typeof(FSH.Modules.MarketIntelligence.MarketIntelligenceModule), 600)]

namespace FSH.Modules.MarketIntelligence
{
#pragma warning disable S2094 // Empty classes should not be used
    public class MarketIntelligenceModule : IModule
    {
        public void ConfigureServices(IHostApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            PermissionConstants.Register(MarketIntelligencePermissions.All);

            builder.Services.AddHeroDbContext<MarketIntelligenceDbContext>();
            builder.Services.AddScoped<IDbInitializer, MarketIntelligenceDbInitializer>();
            builder.Services.AddHttpClient<ICodalClient, CodalClient>(client =>
            {
                client.Timeout = TimeSpan.FromMinutes(3);
            });
            builder.Services.AddScoped<ICodalDisclosureProcessor, ManufacturingMonthlyActivityProcessor>();
            builder.Services.AddScoped<IMonthlySalesParser, MonthlySalesParser>();
            builder.Services.AddHealthChecks()
                .AddDbContextCheck<MarketIntelligenceDbContext>(
                    name: "db:marketintellience",
                    failureStatus: HealthStatus.Unhealthy);
            builder.Services.AddScoped<ICodalCollectorService, CodalCollectorService>();
        }
        public void ConfigureMiddleware(IApplicationBuilder app)
        {
            // No custom middleware needed
        }

        public void MapEndpoints(IEndpointRouteBuilder endpoints)
        {
            ArgumentNullException.ThrowIfNull(endpoints);

            var versionSet = endpoints.NewApiVersionSet()
                .HasApiVersion(new ApiVersion(1))
                .ReportApiVersions()
                .Build();

            var group = endpoints
                .MapGroup("api/v{version:apiVersion}/marketintelligence")
                .WithTags("MarketIntelligence")
                .WithApiVersionSet(versionSet)
                .RequireAuthorization();

            // Trash routes registered first so the literal `/trash` segment wins
            // over the catch-all `/{id:guid}`.
           
            group.MapSearchDisclosuresEndpoint();

            group.MapPost("/codal/import", async (ICodalCollectorService collector,
                                                  CancellationToken ct) =>
            {
                await collector.CollectAsync2(ct);
                return Results.Ok(new
                {
                    message = "Codal import finished"
                });
            }).AllowAnonymous();

            group.MapPost(
        "/codal/parse-pending",
        async (
            ICodalCollectorService collector,
            CancellationToken ct) =>
        {
            await collector.ParsePendingDisclosuresAsync(ct);

            return Results.Ok(new
            {
                message = "Pending disclosures parsed successfully"
            });
        })
        .AllowAnonymous();

        }

    }
#pragma warning restore S2094
}
