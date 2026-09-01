using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Domain;
using FSH.Modules.MarketIntelligence.Domain.Enums;
using FSH.Modules.MarketIntelligence.Services.Codal;
using FSH.Modules.MarketIntelligence.Services.Codal.Configuration;
using FSH.Modules.MarketIntelligence.Services.Codal.Lookups;
using FSH.Modules.MarketIntelligence.Services.Codal.Processors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;

namespace Modules.MarketIntelligence.Services.Codal.Processors;

public sealed class MonthlyActivityBankProcessor(
    HttpClient httpClient,
    MarketIntelligenceDbContext dbContext,
    PreviousYearSummaryLookup previousYearSummaryLookup,
    ILogger<MonthlyActivityBankProcessor> logger)
    : ICodalDisclosureProcessor
{
#pragma warning disable S1075
    private const string CodalBaseUrl = "https://www.codal.ir";
#pragma warning restore S1075

    public bool CanProcess(Disclosure disclosure)
    {
        ArgumentNullException.ThrowIfNull(disclosure);

        CodalDefinitions definitions = CodalDefinitionsProvider.Load();

        return disclosure.Let == 58 &&
               disclosure.Rt == 3 &&
               definitions.MonthlyActivities.TryGetValue(
                   3,
                   out CodalTableDefinition? definition) &&
               definition.Components.Count > 0;
    }

    public async Task ProcessAsync(
        Disclosure disclosure,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(disclosure);

        if (disclosure.Let != 58 ||
            disclosure.Rt != 3)
        {
            return;
        }

        CodalDefinitions definitions = CodalDefinitionsProvider.Load();

        if (!definitions.MonthlyActivities.TryGetValue(
                3,
                out CodalTableDefinition? definition) ||
            definition is null ||
            definition.Components.Count == 0)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(disclosure.Url))
        {
            disclosure.SalesParseStatus =
                DisclosureParseStatus.NoData;

            disclosure.SalesParsedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);

            return;
        }

        try
        {
            Uri reportUri = BuildReportUri(disclosure.Url);

            string html =
                await DownloadHtmlWithRetryAsync(
                    reportUri,
                    cancellationToken);

            /*
             * برای تشخیص ماه اول مالی، یک Component اجباری
             * را به عنوان منبع metadata انتخاب می‌کنیم.
             *
             * در JSON فعلی این Component همان Income / 2548 است.
             */
            CodalTableComponentDefinition? metadataComponent =
                definition.Components.FirstOrDefault(
                    x => x.Required);

            if (metadataComponent is null)
            {
                throw new InvalidOperationException(
                    "Bank definition does not contain a required component.");
            }

            if (!metadataComponent.SelectedCells.TryGetValue(
                    "PeriodAmount",
                    out int metadataPeriodColumn))
            {
                throw new InvalidOperationException(
                    $"PeriodAmount is not defined for bank component " +
                    $"{metadataComponent.Name}.");
            }

            CodalCellResult? metadataCell =
                ReadComponentCell(
                    html,
                    metadataComponent,
                    metadataPeriodColumn);

            if (metadataCell is null ||
                string.IsNullOrWhiteSpace(
                    metadataCell.PeriodEndToDate) ||
                string.IsNullOrWhiteSpace(
                    metadataCell.YearEndToDate))
            {
                disclosure.SalesParseStatus =
                    DisclosureParseStatus.NoData;

                disclosure.SalesParsedAt =
                    DateTime.UtcNow;

                await dbContext.SaveChangesAsync(
                    cancellationToken);

                return;
            }

            string periodEndToDate =
                metadataCell.PeriodEndToDate!;

            string yearEndToDate =
                metadataCell.YearEndToDate!;

            bool isFirstFiscalMonth =
                IsFirstFiscalMonth(
                    periodEndToDate,
                    yearEndToDate);

            decimal periodAmount = 0m;
            decimal yearToDateAmount = 0m;

            bool hasComponentValue = false;

            /*
             * این دو Cell فقط برای metadata مربوط به Summary
             * نگهداری می‌شوند.
             * مبلغ Summary از مجموع همه Componentها ساخته می‌شود.
             */
            CodalCellResult? periodSourceCell = null;
            CodalCellResult? yearToDateSourceCell = null;

            foreach (CodalTableComponentDefinition component
                     in definition.Components)
            {
                if (!component.SelectedCells.TryGetValue(
                        "PeriodAmount",
                        out int normalPeriodColumn))
                {
                    throw new InvalidOperationException(
                        $"PeriodAmount is not defined for bank component " +
                        $"{component.Name}.");
                }

                int periodColumn = normalPeriodColumn;
                                
                CodalCellResult? componentPeriodCell =
                    ReadComponentCell(
                        html,
                        component,
                        periodColumn);

                                               
                /*
                 * Component اختیاری ممکن است اصلاً در گزارش
                 * این بانک وجود نداشته باشد.
                 */
                if (componentPeriodCell is null)
                {
                    if (component.Required)
                    {
                        disclosure.SalesParseStatus =
                            DisclosureParseStatus.NoData;

                        disclosure.SalesParsedAt =
                            DateTime.UtcNow;

                        await dbContext.SaveChangesAsync(
                            cancellationToken);

                        return;
                    }

                    if (logger.IsEnabled(LogLevel.Debug))
                    {
                        logger.LogDebug(
                            "Optional bank component was not found. TracingNo: {TracingNo}, Component: {Component}, MetaTableCode: {MetaTableCode}",
                            disclosure.TracingNo,
                            component.Name,
                            component.MetaTableCode);
                    }

                    continue;
                }

                CodalCellResult? componentYearToDateCell;

                if (isFirstFiscalMonth)
                {
                    componentYearToDateCell =
                        componentPeriodCell;
                }
                else
                {
                    if (!component.SelectedCells.TryGetValue(
                            "YearToDateAmount",
                            out int yearToDateColumn))
                    {
                        throw new InvalidOperationException(
                            $"YearToDateAmount is not defined for bank " +
                            $"component {component.Name}.");
                    }

                    componentYearToDateCell =
                        ReadComponentCell(
                            html,
                            component,
                            yearToDateColumn);
                }

                if (componentYearToDateCell is null)
                {
                    if (component.Required)
                    {
                        disclosure.SalesParseStatus =
                            DisclosureParseStatus.NoData;

                        disclosure.SalesParsedAt =
                            DateTime.UtcNow;

                        await dbContext.SaveChangesAsync(
                            cancellationToken);

                        return;
                    }

                    if (logger.IsEnabled(LogLevel.Debug))
                    {
                        logger.LogDebug(
                            "Optional bank component YTD was not found. TracingNo: {TracingNo}, Component: {Component}, MetaTableCode: {MetaTableCode}",
                            disclosure.TracingNo,
                            component.Name,
                            component.MetaTableCode);
                    }

                    continue;
                }

                if (!decimal.TryParse(
                        componentPeriodCell.Value,
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out decimal componentPeriodAmount))
                {
                    throw new FormatException(
                        $"Invalid PeriodAmount in bank component " +
                        $"{component.Name}. " +
                        $"TracingNo: {disclosure.TracingNo}");
                }

                if (!decimal.TryParse(
                        componentYearToDateCell.Value,
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out decimal componentYearToDateAmount))
                {
                    throw new FormatException(
                        $"Invalid YearToDateAmount in bank component " +
                        $"{component.Name}. " +
                        $"TracingNo: {disclosure.TracingNo}");
                }

                decimal multiplier =
                    component.Operation switch
                    {
                        CodalComponentOperation.Add => 1m,
                        CodalComponentOperation.Subtract => -1m,

                        _ => throw new InvalidOperationException(
                            $"Unsupported operation " +
                            $"{component.Operation} for bank component " +
                            $"{component.Name}.")
                    };

                /*
 * TEMP DEBUG:
 * مقدار هر Component و جمع تجمعی قبل از اعمال آن.
 */
                Console.WriteLine(
                    $"BANK SUM | {component.Name} | " +
                    $"Parsed={componentPeriodAmount} | " +
                    $"Multiplier={multiplier} | " +
                    $"Before={periodAmount}");

                periodAmount +=
                    multiplier * componentPeriodAmount;

                /*
                 * TEMP DEBUG:
                 * جمع تجمعی بعد از اعمال Component.
                 */
                Console.WriteLine(
                    $"BANK SUM | {component.Name} | " +
                    $"After={periodAmount}");

                yearToDateAmount +=
                    multiplier * componentYearToDateAmount;

                hasComponentValue = true;

                /*
                 * Metadata را ترجیحاً از Component اجباری
                 * یعنی Income می‌گیریم.
                 */
                if (component.Required &&
                    periodSourceCell is null)
                {
                    periodSourceCell =
                        componentPeriodCell;

                    yearToDateSourceCell =
                        componentYearToDateCell;
                }
            }

            if (!hasComponentValue ||
                periodSourceCell is null ||
                yearToDateSourceCell is null)
            {
                disclosure.SalesParseStatus =
                    DisclosureParseStatus.NoData;

                disclosure.SalesParsedAt =
                    DateTime.UtcNow;

                await dbContext.SaveChangesAsync(
                    cancellationToken);

                return;
            }

            if (string.IsNullOrWhiteSpace(disclosure.Symbol))
            {
                throw new InvalidOperationException(
                    $"Disclosure {disclosure.TracingNo} " +
                    $"does not have a symbol.");
            }

            decimal? previousYearToDateAmount =
                await previousYearSummaryLookup
                    .FindYearToDateAmountAsync(
                        disclosure.Symbol,
                        periodEndToDate,
                        cancellationToken);

            string symbol = disclosure.Symbol;

            MonthlyActivitySummary? existingSummary =
                await dbContext.MonthlyActivitySummaries
                    .SingleOrDefaultAsync(
                        x =>
                            x.Symbol == symbol &&
                            x.PeriodEndDate == periodEndToDate,
                        cancellationToken);

            if (existingSummary is null)
            {
                var summary =
                    new MonthlyActivitySummary(
                        symbol: disclosure.Symbol,
                        periodEndDate: periodEndToDate,
                        yearEndDate: yearEndToDate,
                        rt: 3,
                        periodAmount: periodAmount,
                        yearToDateAmount: yearToDateAmount,
                        previousYearToDateAmount:
                            previousYearToDateAmount,

                        periodFormula:
                            periodSourceCell.Formula,
                        periodAddress:
                            periodSourceCell.Address,
                        periodRowSequence:
                            periodSourceCell.RowSequence,

                        yearToDateFormula:
                            yearToDateSourceCell.Formula,
                        yearToDateAddress:
                            yearToDateSourceCell.Address,
                        yearToDateRowSequence:
                            yearToDateSourceCell.RowSequence,

                        publishDateTime:
                            disclosure.PublishDateTime,
                        disclosureId:
                            disclosure.Id,
                        tracingNo:
                            disclosure.TracingNo);

                await dbContext.MonthlyActivitySummaries
                    .AddAsync(
                        summary,
                        cancellationToken);
            }
            else if (
                existingSummary.DisclosureId ==
                    disclosure.Id ||
                (disclosure.PublishDateTime.HasValue &&
                 (!existingSummary.PublishDateTime.HasValue ||
                  disclosure.PublishDateTime.Value >
                  existingSummary.PublishDateTime.Value)))
            {
                existingSummary.Update(
                    yearEndDate: yearEndToDate,
                    rt: 3,
                    periodAmount: periodAmount,
                    yearToDateAmount: yearToDateAmount,
                    previousYearToDateAmount:
                        previousYearToDateAmount,

                    periodFormula:
                        periodSourceCell.Formula,
                    periodAddress:
                        periodSourceCell.Address,
                    periodRowSequence:
                        periodSourceCell.RowSequence,

                    yearToDateFormula:
                        yearToDateSourceCell.Formula,
                    yearToDateAddress:
                        yearToDateSourceCell.Address,
                    yearToDateRowSequence:
                        yearToDateSourceCell.RowSequence,

                    publishDateTime:
                        disclosure.PublishDateTime,
                    disclosureId:
                        disclosure.Id,
                    tracingNo:
                        disclosure.TracingNo);
            }

            disclosure.SalesParseStatus =
                DisclosureParseStatus.Success;

            disclosure.SalesParsedAt =
                DateTime.UtcNow;

            await dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode is
                System.Net.HttpStatusCode.NotFound or
                System.Net.HttpStatusCode.Gone)
        {
            disclosure.SalesParseStatus =
                DisclosureParseStatus.NoData;

            disclosure.SalesParsedAt =
                DateTime.UtcNow;

            logger.LogWarning(
                ex,
                "Codal bank report was not found. " +
                "TracingNo: {TracingNo}",
                disclosure.TracingNo);

            await dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            disclosure.SalesParseStatus =
                DisclosureParseStatus.Failed;

            disclosure.SalesParsedAt =
                DateTime.UtcNow;

            logger.LogError(
                ex,
                "Downloading Codal bank report failed. " +
                "TracingNo: {TracingNo}",
                disclosure.TracingNo);

            await dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (FormatException ex)
        {
            disclosure.SalesParseStatus =
                DisclosureParseStatus.Failed;

            disclosure.SalesParsedAt =
                DateTime.UtcNow;

            logger.LogError(
                ex,
                "Invalid numeric value in Codal bank report. " +
                "TracingNo: {TracingNo}",
                disclosure.TracingNo);

            await dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (System.Text.Json.JsonException ex)
        {
            disclosure.SalesParseStatus =
                DisclosureParseStatus.Failed;

            disclosure.SalesParsedAt =
                DateTime.UtcNow;

            logger.LogError(
                ex,
                "Codal bank datasource JSON could not be parsed. " +
                "TracingNo: {TracingNo}",
                disclosure.TracingNo);

            await dbContext.SaveChangesAsync(
                cancellationToken);
        }
    }

    private static CodalCellResult? ReadComponentCell(
        string html,
        CodalTableComponentDefinition component,
        int columnSequence)
    {
        return component.HasTotalRow
            ? CodalCellReader.FindCellValue(
                html,
                component.MetaTableCode,
                columnSequence)
            : CodalCellReader.SumColumnValuesWithoutTotalRow(
                html,
                component.MetaTableCode,
                columnSequence);
    }

    private static Uri BuildReportUri(
        string disclosureUrl)
    {
        if (Uri.TryCreate(
                disclosureUrl,
                UriKind.Absolute,
                out Uri? absoluteUri))
        {
            return absoluteUri;
        }

        return new Uri(
            new Uri(CodalBaseUrl),
            disclosureUrl);
    }

    private async Task<string> DownloadHtmlWithRetryAsync(
        Uri reportUri,
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 3;

        Exception? lastException = null;

        for (int attempt = 1;
             attempt <= maxAttempts;
             attempt++)
        {
            try
            {
                using var request =
                    new HttpRequestMessage(
                        HttpMethod.Get,
                        reportUri);

                request.Version =
                    HttpVersion.Version11;

                request.VersionPolicy =
                    HttpVersionPolicy.RequestVersionOrLower;

                using HttpResponseMessage response =
                    await httpClient.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken);

                if ((int)response.StatusCode == 490)
                {
                    throw new HttpRequestException(
                        "Codal rate limit/security verification " +
                        "triggered (HTTP 490).",
                        null,
                        response.StatusCode);
                }

                response.EnsureSuccessStatusCode();

                return await response.Content
                    .ReadAsStringAsync(
                        cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
                when (ex is
                    HttpRequestException or
                    IOException or
                    OperationCanceledException)
            {
                lastException = ex;

                if (attempt < maxAttempts)
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(
                            attempt * 5),
                        cancellationToken);
                }
            }
        }

        throw new HttpRequestException(
            $"Downloading Codal report failed after " +
            $"{maxAttempts} attempts. Url: {reportUri}",
            lastException);
    }

    private static bool IsFirstFiscalMonth(
        string periodEndToDate,
        string yearEndToDate)
    {
        string[] periodParts =
            periodEndToDate.Split('/');

        string[] yearEndParts =
            yearEndToDate.Split('/');

        int periodYear =
            int.Parse(
                periodParts[0],
                CultureInfo.InvariantCulture);

        int periodMonth =
            int.Parse(
                periodParts[1],
                CultureInfo.InvariantCulture);

        int yearEndYear =
            int.Parse(
                yearEndParts[0],
                CultureInfo.InvariantCulture);

        int yearEndMonth =
            int.Parse(
                yearEndParts[1],
                CultureInfo.InvariantCulture);

        int firstFiscalYear =
            yearEndMonth == 12
                ? yearEndYear
                : yearEndYear - 1;

        int firstFiscalMonth =
            yearEndMonth == 12
                ? 1
                : yearEndMonth + 1;

        return periodYear == firstFiscalYear &&
               periodMonth == firstFiscalMonth;
    }
}