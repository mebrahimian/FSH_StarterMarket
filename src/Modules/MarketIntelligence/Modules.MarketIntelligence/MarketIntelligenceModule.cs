using FSH.Modules.MarketIntelligence.Contracts.Dtos;
using Asp.Versioning;
using FSH.Framework.Jobs.Services;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Modules;
using FSH.Modules.MarketIntelligence.Contracts.Authorization;
using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Features.v1.Disclosures.SearchDisclosures;
using FSH.Modules.MarketIntelligence.Services.Codal;
using FSH.Modules.MarketIntelligence.Services.Codal.DataQuality;
using FSH.Modules.MarketIntelligence.Services.Codal.Interfaces;
using FSH.Modules.MarketIntelligence.Services.Codal.Jobs;
using FSH.Modules.MarketIntelligence.Services.Codal.Lookups;
using FSH.Modules.MarketIntelligence.Services.Codal.Processors;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

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
            //    builder.Services.AddScoped<ICodalDisclosureProcessor, ManufacturingMonthlyActivityProcessor>();
            //    builder.Services.AddScoped<ICodalDisclosureProcessor, RealEstateMonthlyActivityProcessor>();
            builder.Services.AddScoped<PreviousYearSummaryLookup>();
            builder.Services.AddScoped<ICodalDisclosureProcessor, MonthlyActivityProcessor>();
            builder.Services.AddScoped<ICodalDisclosureProcessor, MonthlyActivityType2Processor>();
            builder.Services.AddScoped<ICodalDisclosureProcessor, MonthlyActivityType3Processor>();
            builder.Services.AddScoped<CodalDataQualityAuditService>();

            //    builder.Services.AddScoped<IMonthlySalesParser, MonthlySalesParser>();
            builder.Services.AddHealthChecks()
                .AddDbContextCheck<MarketIntelligenceDbContext>(
                    name: "db:marketintellience",
                    failureStatus: HealthStatus.Unhealthy);
            builder.Services.AddScoped<ICodalCollectorService, CodalCollectorService>();
            builder.Services.AddTransient<CodalBackgroundJob>();
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

            group.MapPost("/codal/newRead", (IJobService jobService) =>
              {
                  string jobId = jobService.Enqueue<CodalBackgroundJob>
                    (job => job.RunIncrementalAsync(CancellationToken.None));

                  return Results.Accepted(value: new
                  { jobId, message = "Codal incremental import queued." });
              }).RequirePermission(MarketIntelligencePermissions
                .CodalOperations
                .Execute);

            group.MapPost("/codal/import", (IJobService jobService) =>
                {
                    string jobId =
                        jobService.Enqueue<CodalBackgroundJob>(
                            job =>
                                job.RunBackfillAsync(CancellationToken.None));

                    return Results.Accepted(
                        value: new
                        { jobId, message = "Codal backfill queued." });
                }).RequirePermission(MarketIntelligencePermissions
                        .CodalOperations
                        .Execute);
            group.MapPost(
    "/codal/symbol-backfill",
    IResult (
        CodalSymbolBackfillRequest request,
        IJobService jobService) =>
    {
        if (
            string.IsNullOrWhiteSpace(
                request.Symbol) ||
            string.IsNullOrWhiteSpace(
                request.FromDate) ||
            string.IsNullOrWhiteSpace(
                request.ToDate))
        {
            return Results.BadRequest(
                new
                {
                    message =
                        "Symbol, fromDate and toDate are required.",
                });
        }

        string symbol =
            request.Symbol.Trim();

        string fromDate =
            request.FromDate.Trim();

        string toDate =
            request.ToDate.Trim();

        if (
            string.Compare(
                fromDate,
                toDate,
                StringComparison.Ordinal) > 0)
        {
            return Results.BadRequest(
                new
                {
                    message =
                        "fromDate cannot be after toDate.",
                });
        }

        string jobId =
            jobService.Enqueue<CodalBackgroundJob>(
                job =>
                    job.RunSymbolBackfillAsync(
                        symbol,
                        fromDate,
                        toDate));

        return Results.Accepted(
            value: new
            {
                jobId,
                message =
                    "Targeted Codal backfill queued.",
            });
    })
    .WithName("QueueCodalSymbolBackfill")
    .WithSummary(
        "Queues targeted Codal backfill for one symbol and date range")
    .RequirePermission(
        MarketIntelligencePermissions
            .CodalOperations
            .Execute);
            group.MapPost(
                "/codal/parse-pending",
                (IJobService jobService) =>
                {
                    string jobId =
                        jobService.Enqueue<CodalBackgroundJob>(
                            job =>
                                job.RunParsePendingAsync(CancellationToken.None));

                    return Results.Accepted(
                        value: new
                        {
                            jobId,
                            message =
                                "Pending disclosure parsing queued."
                        });
                })
                .RequirePermission(MarketIntelligencePermissions
                                  .CodalOperations
                                  .Execute);

            group.MapGet(
                "/codal/jobs/{jobId}/status",
                (string jobId) =>
                {
                    using var connection = JobStorage.Current.GetConnection();

                    var jobData = connection.GetJobData(jobId);

                    if (jobData is null)
                    {
                        return Results.NotFound(
                            new
                            {
                                jobId,
                                message = "Job not found."
                            });
                    }

                    var state = connection.GetStateData(jobId);

                    return Results.Ok(
                        new
                        {
                            jobId,
                            status = state?.Name ?? "Unknown",
                            reason = state?.Reason,
                            createdAt = jobData.CreatedAt
                        });
                }).RequirePermission(MarketIntelligencePermissions
                                    .CodalOperations
                                    .Execute);
            group.MapGet(
        "/codal/data-quality",
        async (
            int? coverageYears,
            CodalDataQualityAuditService auditService,
            CancellationToken cancellationToken) =>
        {
            int requestedYears =
                coverageYears ?? 5;

            if (requestedYears is < 1 or > 20)
            {
                return Results.BadRequest(
                    new
                    {
                        message =
                            "Coverage years must be between 1 and 20.",
                    });
            }

            CodalDataQualityReport report = await auditService.RunAsync(
                         coverageYears: requestedYears,
                         cancellationToken: cancellationToken);

            return Results.Ok(report);
        })
    .WithName("GetCodalDataQuality")
    .WithSummary(
        "Audits Codal disclosure and monthly summary data quality")
    .RequirePermission(
        MarketIntelligencePermissions
            .CodalOperations
            .Execute);
        }

    }
#pragma warning restore S2094
}
