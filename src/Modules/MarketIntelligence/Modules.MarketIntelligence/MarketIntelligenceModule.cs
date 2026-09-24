using Asp.Versioning;
using FSH.Framework.Jobs.Services;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Modules;
using FSH.Modules.MarketIntelligence.Contracts.Authorization;
using FSH.Modules.MarketIntelligence.Contracts.Dtos;
using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Features.v1.CodalIncrementalSchedule;
using FSH.Modules.MarketIntelligence.Features.v1.DataQualityIssues;
using FSH.Modules.MarketIntelligence.Features.v1.Disclosures.SearchDisclosures;
using FSH.Modules.MarketIntelligence.Features.v1.FiscalYearSales;
using FSH.Modules.MarketIntelligence.Features.v1.PortfolioMatching;
using FSH.Modules.MarketIntelligence.Features.v1.PortfolioViewer;
using FSH.Modules.MarketIntelligence.Services.Codal;
using FSH.Modules.MarketIntelligence.Services.Codal.Configuration;
using FSH.Modules.MarketIntelligence.Services.Codal.DataQuality;
using FSH.Modules.MarketIntelligence.Services.Codal.Interfaces;
using FSH.Modules.MarketIntelligence.Services.Codal.Jobs;
using FSH.Modules.MarketIntelligence.Services.Codal.Lookups;
using FSH.Modules.MarketIntelligence.Services.Codal.Portfolio;
using FSH.Modules.MarketIntelligence.Services.Codal.Processors;
using FSH.Modules.MarketIntelligence.Services.Companies;
using FSH.Modules.MarketIntelligence.Services.Insights;
using FSH.Modules.MarketIntelligence.Services.Insights.Jobs;
using FSH.Modules.MarketIntelligence.Services.MarketData;
using FSH.Modules.MarketIntelligence.Services.Portfolio;
using FSH.Modules.MarketIntelligence.Services.Tsetmc;
using FSH.Modules.MarketIntelligence.Services.Tsetmc.Jobs;
using Hangfire;
using Hangfire.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Modules.MarketIntelligence.Services.Codal.Processors;

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

            builder.Services.Configure<CodalOptions>(
                    builder.Configuration.GetSection(CodalOptions.SectionName));
            builder.Services.AddHeroDbContext<MarketIntelligenceDbContext>();
            builder.Services.AddScoped<IDbInitializer, MarketIntelligenceDbInitializer>();
            builder.Services.AddSingleton<CodalRequestGate>();
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
            builder.Services.AddScoped<ICodalDisclosureProcessor, MonthlyActivityBankProcessor>();
            builder.Services.AddScoped<InvestmentPortfolioReader>();
            builder.Services.AddScoped<ICodalDisclosureProcessor, InvestmentPortfolioProcessor>();
            builder.Services.AddScoped<CodalDataQualityAuditService>();
            builder.Services.AddScoped<IMarketPriceProvider, BorsMarketPriceProvider>();
            builder.Services.AddScoped<ICompanyRegistry, BorsCompanyRegistry>();
            builder.Services.AddScoped<CompanyIdBackfillService>();
            builder.Services.AddScoped<SalesPerformanceAnalyzer>();
            builder.Services.AddScoped<SalesPerformanceSnapshotService>();
            builder.Services.AddScoped<SalesPerformanceSnapshotRebuildService>();
            builder.Services.AddScoped<DisclosureInsightPipeline>();          
            builder.Services.AddScoped<IDisclosureInsightProcessor, SalesPerformanceInsightProcessor>();
            builder.Services.AddScoped<PortfolioHoldingAssetBootstrapService>();
            builder.Services.AddOptions<TsetmcOptions>().BindConfiguration(TsetmcOptions.SectionName);
            builder.Services.AddScoped<IPortfolioChildCompanyResolver, PortfolioChildCompanyResolver>();

            builder.Services.AddHealthChecks()
                .AddDbContextCheck<MarketIntelligenceDbContext>(
                    name: "db:marketintellience",
                    failureStatus: HealthStatus.Unhealthy);
            builder.Services.AddScoped<ICodalCollectorService, CodalCollectorService>();
            builder.Services.AddScoped<SalesRecordBrokenDetector>();
            builder.Services.AddTransient<CodalBackgroundJob>();
            builder.Services.AddTransient<TsetmcBackgroundJob>();
            builder.Services.AddTransient<SalesPerformanceSnapshotRebuildJob>();
            builder.Services.AddScoped<CodalCompanyReferenceSyncService>();
            builder.Services.AddScoped<TsetmcPriceReader>();
            builder.Services.AddScoped<PriceHistoryCollectorService>();
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
            group.MapGetFiscalYearSalesEndpoint();
            group.MapGetPortfolioByDisclosureIdEndpoint();
            group.MapGetDataQualityIssuesEndpoint();
            group.MapGetCodalIncrementalScheduleEndpoint();
            group.MapUpdateCodalIncrementalScheduleEndpoint();
            group.MapGetUnmatchedPortfolioCompaniesEndpoint();
            group.MapGetPortfolioCompanyUsageEndpoint();
            group.MapSearchPortfolioCompaniesEndpoint();
            group.MapCreateUnlistedPortfolioCompanyEndpoint();

            group.MapMatchPortfolioCompanyEndpoint();
            group.MapUnmatchPortfolioCompanyEndpoint();

            group.MapGetMatchedPortfolioCompaniesEndpoint();

            group.MapPost("/codal/newRead", (IJobService jobService) =>
              {
                  string jobId = jobService.Enqueue<CodalBackgroundJob>
                    (job => job.RunIncrementalAsync(CancellationToken.None));

                  return Results.Accepted(value: new
                  { jobId, message = "Codal incremental import queued." });
              }).RequirePermission(MarketIntelligencePermissions
                .CodalOperations
                .Execute);

            ////////////////////////////
            group.MapPost("/codal/backfill",
                    IResult (IJobService jobService,
                             IConfiguration configuration) =>
               {
                   int startPage =
                     configuration.GetValue<int?>("MarketIntelligence:Codal:BackfillStartPage") ?? 4000;

                   int endPage = Math.Max(1, startPage - 199);

                   string jobId = jobService.Enqueue<CodalBackgroundJob>(
                      job => job.RunBackfillChunkAsync(startPage, endPage,
                                 CancellationToken.None));

                   return Results.Accepted(value: new
                   {
                       jobId,
                       startPage,
                       endPage,
                       message = "Codal backfill queued.",
                   });
               }).RequirePermission(MarketIntelligencePermissions.CodalOperations.Execute);
            ////////////////////////////
            group.MapPost("/codal/symbol-backfill", IResult
                     (CodalSymbolBackfillRequest request, IJobService jobService) =>
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
                }).WithName("QueueCodalSymbolBackfill")
                  .WithSummary("Queues targeted Codal backfill for one symbol and date range")
                  .RequirePermission(MarketIntelligencePermissions
                          .CodalOperations
                          .Execute);
            ////////
            group.MapPost("/codal/symbol-backfill/direct", async Task<IResult> (
                  CodalSymbolBackfillRequest request,
                  [FromServices] ICodalCollectorService codalCollectorService,
                  CancellationToken cancellationToken) =>
                    {
                      if (
                           string.IsNullOrWhiteSpace(request.Symbol) ||
                           string.IsNullOrWhiteSpace(request.FromDate) ||
                           string.IsNullOrWhiteSpace(request.ToDate))
                      {
                        return Results.BadRequest(
                            new
                                 {
                                    message = "Symbol, fromDate and toDate are required.",
                                 });
                      }

                      string symbol = request.Symbol.Trim();

                      string fromDate = request.FromDate.Trim();

                      string toDate = request.ToDate.Trim();

                      if (string.Compare(fromDate, toDate, StringComparison.Ordinal) > 0)
                           {
                              return Results.BadRequest(
                                 new
                                    {
                                       message = "fromDate cannot be after toDate.",
                                    });
                           }

                  await codalCollectorService.CollectSymbolBackfillAsync(
                               symbol,
                               fromDate,
                               toDate,
                               cancellationToken);

                   return Results.Ok(new
                                          {
                                             message = "Targeted Codal backfill completed.",
                                          });
            }).WithName("RunCodalSymbolBackfill")
              .WithSummary("Runs targeted Codal backfill directly for one symbol and date range")
              .RequirePermission(
                   MarketIntelligencePermissions
                   .CodalOperations
                   .Execute);
            ////////////////////
            group.MapPost("/codal/parse-pending", (IJobService jobService) =>
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
            group.MapPost("/tsetmc/prices/backfill/{instrumentId:int}",
                  async Task<IResult> (int instrumentId,
                        PriceHistoryCollectorService collectorService,
                        CancellationToken cancellationToken) =>
            {
                var result = await collectorService
                      .BackfillInstrumentAsync(instrumentId, cancellationToken)
                      .ConfigureAwait(false);

                int inserted = result.Inserted;

                return Results.Ok(new
                         { instrumentId, inserted });
            });
            group.MapPost(
    "/tsetmc/incremental/test______________",
    async (
        PriceHistoryCollectorService collectorService,
        CancellationToken cancellationToken) =>
    {
        int processed =
            await collectorService
                .RunIncrementalAsync(cancellationToken)
                .ConfigureAwait(false);

        return Results.Ok(new
        {
            processed
        });
    });
            ////////////////////
            ///// TO DO: Remove after CompanyId + Insight pipeline is fully integrated.
            ///////////////////////////////////
            
            group.MapGet("/insights/test-sales-record", async (
                  string symbol,
                  string periodEndDate,
                  SalesRecordBrokenDetector detector,
                  CancellationToken cancellationToken) =>
                  {
                     SalesRecordBrokenResult? result =
                         await detector.DetectAsync(
                            symbol,
                            periodEndDate,
                            cancellationToken);

                     return result is null
                       ? Results.NotFound(
                              new
                                  {
                                     symbol,
                                     periodEndDate,
                                     message = "No 12-month sales record was detected."
                                  })
                       : Results.Ok(result);
                  })
                  .WithName("TestSalesRecordBroken")
                  .WithSummary("Tests the SalesRecordBroken insight detector")
                  .RequirePermission(MarketIntelligencePermissions
                                     .CodalOperations
                                     .Execute);
            group.MapGet("/companies/test-resolve", async (
                  string symbol,
                  ICompanyRegistry companyRegistry,
                  CancellationToken cancellationToken) =>
                    {
                       CompanyIdentity? company = await companyRegistry.FindBySymbolAsync(
                          symbol, cancellationToken);

                       return company is null
                           ? Results.NotFound(new
                                  {
                                     symbol,
                                     message = "Company was not found."
                                  })
                           : Results.Ok(company);
                    }).WithName("TestCompanyResolve")
                      .WithSummary("Tests company resolution by symbol")
                      .RequirePermission(MarketIntelligencePermissions
                                         .CodalOperations
                                         .Execute);
            // TO DO: Remove after CompanyId historical backfill is completed.
            group.MapPost("/companies/backfill-company-id",
                async (
                    CompanyIdBackfillService backfillService,
                    CancellationToken cancellationToken) =>
                {
                    CompanyIdBackfillResult result =
                        await backfillService.RunAsync(
                            cancellationToken);

                    return Results.Ok(result);
                })
                .WithName("BackfillDisclosureCompanyIds")
                .WithSummary(
                    "Backfills CompanyId for historical disclosures")
                .RequirePermission(
                    MarketIntelligencePermissions
                        .CodalOperations
                        .Execute);
            //////////////////////////////
            // End of To do Remove
            ///////////////////////
            group.MapGet("/codal/jobs/{jobId}/status", (string jobId) =>
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

            group.MapPost("/insights/sales-performance/rebuild-missing",
                       (IJobService jobService) =>
                       {
                         string jobId =
                                  jobService.Enqueue<SalesPerformanceSnapshotRebuildJob>(
                                  job => job.RunMissingAsync(0, CancellationToken.None));

                return Results.Accepted(
                        value: new
                            {
                               jobId,
                               message = "Sales performance missing snapshots rebuild queued.",
                            });
             }).WithName("RebuildMissingSalesPerformanceSnapshots")
               .WithSummary("Rebuilds missing sales performance snapshots")
               .RequirePermission(MarketIntelligencePermissions
                                  .CodalOperations
                                  .Execute);
            group.MapPost("/codal/companies/sync", async Task<IResult> (
                       CodalCompanyReferenceSyncService syncService,
                       CancellationToken cancellationToken) =>
                { await syncService
                           .SyncAsync(cancellationToken)
                           .ConfigureAwait(false);

                  return Results.Ok( new
                                        {
                                           message = "Codal companies synchronized successfully."
                                        });
                });
            /////////
            group.MapPost("/portfolio/holding-assets/bootstrap",
                  async (
                          PortfolioHoldingAssetBootstrapService bootstrapService,
                          CancellationToken cancellationToken) =>
                    {
                       PortfolioHoldingAssetBootstrapResult result =
                           await bootstrapService.RunAsync(cancellationToken);

                  return Results.Ok(result);
                }).WithName("BootstrapPortfolioHoldingAssets")
                  .WithSummary("Creates portfolio holding assets from existing company aliases")
                  .RequirePermission(MarketIntelligencePermissions
                                     .CodalOperations
                                     .Execute);
            //////////////
            /////////////
            group.MapGet("/insights/sales-performance/rebuild-status",  async (
                  MarketIntelligenceDbContext dbContext,
                  CancellationToken cancellationToken) =>
                {
                  int total = await dbContext
                         .MonthlyActivitySummaries
                         .AsNoTracking()
                         .Where(x =>
                                     x.CompanyId.HasValue &&
                                     x.PeriodAmount.HasValue &&
                                     x.PeriodEndDate.Length >= 7)
                         .Select(x => new
                                        {
                                           CompanyId = x.CompanyId!.Value,
                                           x.PeriodEndDate,
                                        })
                         .Distinct()
                         .CountAsync(cancellationToken);

                int completed = await dbContext.SalesPerformanceSnapshots
                         .AsNoTracking()
                         .CountAsync(cancellationToken);

                int remaining = Math.Max(total - completed, 0);

                decimal progressPercent = total == 0
                      ? 100m
                      : Math.Round(completed * 100m / total, 2);

            return Results.Ok(new
                 {
                    total,
                    completed,
                    remaining,
                    progressPercent,
                    isComplete = remaining == 0,
                 });
             }).WithName("GetSalesPerformanceRebuildStatus")
               .WithSummary("Gets sales performance snapshot rebuild progress")
               .RequirePermission(MarketIntelligencePermissions
                                  .CodalOperations
                                  .Execute);

            group.MapGet("/codal/data-quality", async (
                                              int? coverageYears,
                                              CodalDataQualityAuditService auditService,
                                              CancellationToken cancellationToken) =>
            {
                int requestedYears = coverageYears ?? 5;

                if (requestedYears is < 1 or > 20)
                {
                    return Results.BadRequest(
                       new
                       {
                           message = "Coverage years must be between 1 and 20.",
                       });
                }

                CodalDataQualityReport report = await auditService.RunAsync(
                         coverageYears: requestedYears,
                         cancellationToken: cancellationToken);

                return Results.Ok(report);
            }).WithName("GetCodalDataQuality")
              .WithSummary("Audits Codal disclosure and monthly summary data quality")
              .RequirePermission(MarketIntelligencePermissions
              .CodalOperations
              .Execute);

            var jobManager = endpoints.ServiceProvider
                                      .GetService<IRecurringJobManager>();

            if (jobManager is not null)
            {
                jobManager.AddOrUpdate(
                    "market-intelligence-codal-incremental",
                    Job.FromExpression<CodalBackgroundJob>(
                        job => job.RunScheduledIncrementalAsync(
                            CancellationToken.None)),
                    "*/5 * * * *",
                    new RecurringJobOptions
                    {
                        TimeZone = TimeZoneInfo.Utc,
                    });

                jobManager.AddOrUpdate(
                    "market-intelligence-tsetmc-incremental-morning",
                    Job.FromExpression<TsetmcBackgroundJob>(
                        job => job.RunIncrementalAsync(
                            CancellationToken.None)),
                    "3,13,23,33,43,53 9-11 * * 0-3,6",
                    new RecurringJobOptions
                    {
                        TimeZone =
                            TimeZoneInfo.FindSystemTimeZoneById(
                                "Iran Standard Time"),
                    });

                jobManager.AddOrUpdate(
                    "market-intelligence-tsetmc-incremental-noon",
                    Job.FromExpression<TsetmcBackgroundJob>(
                        job => job.RunIncrementalAsync(
                            CancellationToken.None)),
                    "3,13,23 12 * * 0-3,6",
                    new RecurringJobOptions
                    {
                        TimeZone =
                            TimeZoneInfo.FindSystemTimeZoneById(
                                "Iran Standard Time"),
                    });

                jobManager.AddOrUpdate(
                    "market-intelligence-tsetmc-incremental-with-share-changes",
                    Job.FromExpression<TsetmcBackgroundJob>(
                        job => job.RunPriceIncrementalWithShareChangesAsync(
                            CancellationToken.None)),
                    "33 12 * * 0-3,6",
                    new RecurringJobOptions
                    {
                        TimeZone =
                            TimeZoneInfo.FindSystemTimeZoneById(
                                "Iran Standard Time"),
                    });
                         
            }

        }



    }

#pragma warning restore S2094
}



// Cron format:  فرمت زمانبندی جاب
// Minute  Hour  DayOfMonth  Month  DayOfWeek
//
// خواسته                         Cron               معنی
// --------------------------------------------------------------------------------
// هر ۳۰ دقیقه                    */30 * * * *        :00 و :30 هر ساعت
// هر ساعت                        0 * * * *           سر هر ساعت
// روزی یک بار ساعت 8             0 8 * * *           هر روز 08:00
// روزی دو بار                    0 8,18 * * *        هر روز 08:00 و 18:00
// هر روز ساعت 8 و 12 و 16        0 8,12,16 * * *     سه بار در روز
// هر هفته شنبه ساعت 9            0 9 * * 6           شنبه‌ها 09:00
// هر هفته دوشنبه ساعت 10         0 10 * * 1          دوشنبه‌ها
// شنبه تا چهارشنبه ساعت 8        0 8 * * 6-3         بهتر است به دلیل عبور از انتهای هفته جدا نوشته شود
// دوشنبه تا جمعه ساعت 8          0 8 * * 1-5         روزهای کاری متداول
// اول هر ماه ساعت 7              0 7 1 * *           روز اول ماه
// پانزدهم هر ماه ساعت 7          0 7 15 * *          روز 15
// ماهی دو بار                    0 8 1,15 * *        اول و پانزدهم ماه ساعت 8
// آخر هر ماه                     -                   با Cron ساده بهتر است منطق مخصوص داشته باشد
// هر سه ماه، روز اول             0 8 1 */3 *         فصل‌وار
// اول ژانویه هر سال              0 8 1 1 *           سالی یک بار
//
// علائم:
// *      = همه
// */30   = هر 30 واحد
// 5-10   = از 5 تا 10
// 1,15   = 1 و 15
