using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Domain;
using FSH.Modules.MarketIntelligence.Domain.Enums;
using FSH.Modules.MarketIntelligence.Services.Codal.Configuration;
using FSH.Modules.MarketIntelligence.Services.Codal.Processors;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using FSH.Modules.MarketIntelligence.Services.Codal.Lookups;
using FSH.Modules.MarketIntelligence.Utilities;
namespace FSH.Modules.MarketIntelligence.Services.Codal.Processors;

public sealed class MonthlyActivityType2Processor(
    HttpClient httpClient,
    MarketIntelligenceDbContext dbContext,
    PreviousYearSummaryLookup previousYearSummaryLookup,
    ILogger<MonthlyActivityType2Processor> logger)
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
               disclosure.Rt is byte rt && 
               rt == 6 && 
               definitions.MonthlyActivities.ContainsKey(rt);
    }

    public async Task ProcessAsync(
    Disclosure disclosure,
    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(disclosure);

        if (disclosure.Let != 58 ||
            disclosure.Rt is not byte rt)
        {
            return;
        }

        CodalDefinitions definitions = CodalDefinitionsProvider.Load();

        if (!definitions.MonthlyActivities.TryGetValue(
                rt,
                out CodalTableDefinition? definition)
            || definition is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(disclosure.Url))
        {
            disclosure.SalesParseStatus = DisclosureParseStatus.NoData;

            disclosure.SalesParsedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);

            return;
        }

        try
        {
            Uri reportUri = BuildReportUri(disclosure.Url);

            string html = await DownloadHtmlWithRetryAsync(
                reportUri,
                cancellationToken);

            // فقط برای گرفتن PeriodEndToDate و YearEndToDate.
            // Value این سلول در ماه اول قابل استفاده نیست.
            CodalCellResult? metadataCell =
                CodalCellReader.FindCellValue(
                    html,
                    definition.MetaTableCode,
                    definition.SelectedCells["PeriodAmount"]);

            if (metadataCell is null ||
                string.IsNullOrWhiteSpace(metadataCell.PeriodEndToDate) ||
                string.IsNullOrWhiteSpace(metadataCell.YearEndToDate))
            {
                disclosure.SalesParseStatus = DisclosureParseStatus.NoData;
                disclosure.SalesParsedAt = DateTime.UtcNow;

                await dbContext.SaveChangesAsync(cancellationToken);

                return;
            }

            string periodEndToDate = metadataCell.PeriodEndToDate!;
            string yearEndToDate = metadataCell.YearEndToDate!;

            bool isFirstFiscalMonth =
                IsFirstFiscalMonth(
                    periodEndToDate,
                    yearEndToDate);

            int periodColumn =
                definition.SelectedCells["PeriodAmount"];
                        
            CodalCellResult? periodCell =
                CodalCellReader.FindCellValue(
                    html,
                    definition.MetaTableCode,
                    periodColumn);

            CodalCellResult? periodCellPlus2 =
                CodalCellReader.FindCellValue(
                    html,
                    definition.MetaTableCode,
                    periodColumn + 2);

            if (periodCell is null ||
                periodCellPlus2 is null)
            {
                disclosure.SalesParseStatus = DisclosureParseStatus.NoData;
                disclosure.SalesParsedAt = DateTime.UtcNow;

                await dbContext.SaveChangesAsync(cancellationToken);

                return;
            }

            if (string.IsNullOrWhiteSpace(disclosure.Symbol))
            {
                throw new InvalidOperationException(
                    $"Disclosure {disclosure.TracingNo} does not have a symbol.");
            }

            decimal periodAmount =
                ParseDecimal(
                    periodCell.Value,
                    disclosure.Symbol,
                    disclosure.PublishDateTimeRaw,
                    "PeriodBimeh")
                -
                ParseDecimal(
                    periodCellPlus2.Value,
                    disclosure.Symbol,
                    disclosure.PublishDateTimeRaw,
                    "PeriodKhesarat");

            CodalCellResult yearToDateCell;
            decimal yearToDateAmount;

            if (isFirstFiscalMonth)
            {
                yearToDateCell = periodCell;
                yearToDateAmount = periodAmount;
            }
            else
            {
                CodalCellResult? currentYearToDateCell =
                    CodalCellReader.FindCellValue(
                        html,
                        definition.MetaTableCode,
                        definition.SelectedCells["YearToDateAmount"]);

                CodalCellResult? yearToDateCellPlus2 =
                    CodalCellReader.FindCellValue(
                        html,
                        definition.MetaTableCode,
                        definition.SelectedCells["YearToDateAmount"] + 2);

                if (currentYearToDateCell is null ||
                    yearToDateCellPlus2 is null)
                {
                    disclosure.SalesParseStatus = DisclosureParseStatus.NoData;
                    disclosure.SalesParsedAt = DateTime.UtcNow;

                    await dbContext.SaveChangesAsync(cancellationToken);

                    return;
                }

                yearToDateCell = currentYearToDateCell;

                yearToDateAmount =
                    ParseDecimal(
                        currentYearToDateCell.Value,
                        disclosure.Symbol,
                        disclosure.PublishDateTimeRaw,
                        "YearToDateBimeh")
                    -
                    ParseDecimal(
                        yearToDateCellPlus2.Value,
                        disclosure.Symbol,
                        disclosure.PublishDateTimeRaw,
                        "YearToDateKhesarat");
            }

            decimal? previousYearToDateAmount = null;

            bool hasPreviousYearCellDefinition =
                definition.SelectedCells.TryGetValue(
                    "PreviousYearToDateAmount",
                    out int previousYearToDateCellIndex);

            previousYearToDateAmount = await previousYearSummaryLookup
                .FindYearToDateAmountAsync(
                    disclosure.Symbol,
                    periodEndToDate,
                    cancellationToken);

            MonthlyActivitySummary? existingSummary =
                await dbContext.MonthlyActivitySummaries
                    .SingleOrDefaultAsync(
                        x =>
                            x.Symbol == disclosure.Symbol &&
                            x.PeriodEndDate == periodEndToDate,
                        cancellationToken);

            if (existingSummary is null)
            {
                var summary =
                    new MonthlyActivitySummary(
                        symbol: disclosure.Symbol,
                        periodEndDate: periodEndToDate,
                        yearEndDate: yearEndToDate,
                        rt: rt,
                        periodAmount: periodAmount,
                        yearToDateAmount: yearToDateAmount,
                        previousYearToDateAmount: previousYearToDateAmount,
                        periodFormula: periodCell.Formula,
                        periodAddress: periodCell.Address,
                        periodRowSequence: periodCell.RowSequence,
                        yearToDateFormula: yearToDateCell.Formula,
                        yearToDateAddress: yearToDateCell.Address,
                        yearToDateRowSequence: yearToDateCell.RowSequence,
                        publishDateTime: disclosure.PublishDateTime,
                        disclosureId: disclosure.Id,
                        tracingNo: disclosure.TracingNo);

                await dbContext.MonthlyActivitySummaries
                    .AddAsync(
                        summary,
                        cancellationToken);
            }
            else if (existingSummary.DisclosureId == disclosure.Id ||
                     (disclosure.PublishDateTime.HasValue &&
                      (!existingSummary.PublishDateTime.HasValue ||
                       disclosure.PublishDateTime.Value >
                       existingSummary.PublishDateTime.Value)))
            {
                existingSummary.Update(
                    yearEndDate: yearEndToDate,
                    rt: rt,

                    periodAmount: periodAmount,
                    yearToDateAmount: yearToDateAmount,
                    previousYearToDateAmount: previousYearToDateAmount,
                    periodFormula: periodCell.Formula,
                    periodAddress: periodCell.Address,
                    periodRowSequence: periodCell.RowSequence,

                    yearToDateFormula: yearToDateCell.Formula,
                    yearToDateAddress: yearToDateCell.Address,
                    yearToDateRowSequence: yearToDateCell.RowSequence,

                    publishDateTime: disclosure.PublishDateTime,
                    disclosureId: disclosure.Id,
                    tracingNo: disclosure.TracingNo);
            }

            disclosure.SalesParseStatus = DisclosureParseStatus.Success;
            disclosure.SalesParsedAt = DateTime.UtcNow;

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
            disclosure.SalesParseStatus = DisclosureParseStatus.NoData;

            disclosure.SalesParsedAt = DateTime.UtcNow;

            logger.LogWarning(
                ex,
                "Codal report was not found. TracingNo: {TracingNo}",
                disclosure.TracingNo);

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            disclosure.SalesParseStatus =
                DisclosureParseStatus.Failed;

            disclosure.SalesParsedAt =
                DateTime.UtcNow;

            logger.LogError(
                ex,
                "Downloading Codal report failed. TracingNo: {TracingNo}",
                disclosure.TracingNo);

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (FormatException ex)
        {
            disclosure.SalesParseStatus =
                DisclosureParseStatus.Failed;

            disclosure.SalesParsedAt =
                DateTime.UtcNow;

            logger.LogError(
                ex,
                "Invalid numeric value in Codal report. TracingNo: {TracingNo}",
                disclosure.TracingNo);

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (System.Text.Json.JsonException ex)
        {
            disclosure.SalesParseStatus =
                DisclosureParseStatus.Failed;

            disclosure.SalesParsedAt =
                DateTime.UtcNow;

            logger.LogError(
                ex,
                "Codal datasource JSON could not be parsed. TracingNo: {TracingNo}",
                disclosure.TracingNo);

            await dbContext.SaveChangesAsync(cancellationToken);
        }

    }

    private static Uri BuildReportUri(string disclosureUrl)
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

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    reportUri);

                request.Version = HttpVersion.Version11;
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
                        "Codal rate limit/security verification triggered (HTTP 490).",
                        null,
                        response.StatusCode);
                }
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync(
                    cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
                when (ex is HttpRequestException
                      or IOException
                      or OperationCanceledException)
            {
                lastException = ex;

                // فقط وقتی هنوز تلاش دیگری باقی مانده صبر کن
                if (attempt < maxAttempts)
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(attempt * 5),
                        cancellationToken);
                }
            }
        }

        throw new HttpRequestException(
            $"Downloading Codal report failed after {maxAttempts} attempts. " +
            $"Url: {reportUri}",
            lastException);
    }
    private static bool IsFirstFiscalMonth(
    string periodEndToDate,
    string yearEndToDate)
    {
        string[] periodParts = periodEndToDate.Split('/');
        string[] yearEndParts = yearEndToDate.Split('/');

        int periodYear = int.Parse(
            periodParts[0],
            CultureInfo.InvariantCulture);

        int periodMonth = int.Parse(
            periodParts[1],
            CultureInfo.InvariantCulture);

        int yearEndYear = int.Parse(
            yearEndParts[0],
            CultureInfo.InvariantCulture);

        int yearEndMonth = int.Parse(
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
    private static decimal ParseDecimal(
    string? value,
    string? symbol,
    string? pubDate,
    string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        string normalizedValue = PersianTextNormalizer.NormalizeNumber(
                value);

        if (!decimal.TryParse(
                normalizedValue,
                NumberStyles.Number |
                NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out decimal result))
        {
            throw new InvalidOperationException(
                $"{fieldName} value '{value}' is invalid " +
                $"for disclosure {symbol}{pubDate}.");
        }

        return result;
    }

}