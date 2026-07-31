using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Domain;
using FSH.Modules.MarketIntelligence.Domain.Enums;
using FSH.Modules.MarketIntelligence.Services.Codal.Configuration;
using FSH.Modules.MarketIntelligence.Services.Codal.Processors;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;

namespace FSH.Modules.MarketIntelligence.Services.Codal.Processors;

public sealed class MonthlyActivityProcessor(
    HttpClient httpClient,
    MarketIntelligenceDbContext dbContext)
    : ICodalDisclosureProcessor
{

#pragma warning disable S1075
    private const string CodalBaseUrl = "https://www.codal.ir";
#pragma warning restore S1075

    public bool CanProcess(Disclosure disclosure)
    {
        ArgumentNullException.ThrowIfNull(disclosure);

        CodalDefinitions definitions = CodalDefinitionsProvider.Load();

        return disclosure.Let == 58
            && disclosure.Rt is byte rt
            && definitions.MonthlyActivities.ContainsKey(rt);
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

        CodalDefinitions definitions =
            CodalDefinitionsProvider.Load();

        if (!definitions.MonthlyActivities.TryGetValue(
                rt,
                out CodalTableDefinition? definition)
            || definition is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(disclosure.Url))
        {
            disclosure.SalesParseStatus =
                DisclosureParseStatus.Failed;

            disclosure.SalesParsedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(
                cancellationToken);

            return;
        }

        try
        {
            Uri reportUri =
                BuildReportUri(disclosure.Url);

            string html =
                await DownloadHtmlWithRetryAsync(
                    reportUri,
                    cancellationToken);

            CodalCellResult? periodCell =
                CodalCellFinder.FindCellValue(
                    html,
                    definition.MetaTableId,
                    definition.MetaTableCode,
                    definition.SelectedCells["PeriodAmount"]);

            CodalCellResult? yearToDateCell =
                CodalCellFinder.FindCellValue(
                    html,
                    definition.MetaTableId,
                    definition.MetaTableCode,
                    definition.SelectedCells["YearToDateAmount"]);

            if (periodCell is null ||
                yearToDateCell is null)
            {
                throw new InvalidOperationException(
                    $"Monthly activity cells were not found for disclosure {disclosure.TracingNo}.");
            }

            if (string.IsNullOrWhiteSpace(
                    periodCell.PeriodEndToDate))
            {
                throw new InvalidOperationException(
                    $"Period end date was not found for disclosure {disclosure.TracingNo}.");
            }

            if (string.IsNullOrWhiteSpace(
                    disclosure.Symbol))
            {
                throw new InvalidOperationException(
                    $"Disclosure {disclosure.TracingNo} does not have a symbol.");
            }

            decimal periodAmount = ParseDecimal(periodCell.Value,
                                                disclosure.TracingNo,
                                                "PeriodAmount");

            decimal yearToDateAmount = ParseDecimal(yearToDateCell.Value,
                                                    disclosure.TracingNo,
                                                    "YearToDateAmount");

            decimal? previousYearToDateAmount = null;

            bool hasPreviousYearCellDefinition =
                definition.SelectedCells.TryGetValue(
                    "PreviousYearToDateAmount",
                    out int previousYearToDateCellIndex);

            if (hasPreviousYearCellDefinition)
            {
                CodalCellResult? previousYearToDateCell =
                    CodalCellFinder.FindCellValue(
                        html,
                        definition.MetaTableId,
                        definition.MetaTableCode,
                        previousYearToDateCellIndex);

                if (previousYearToDateCell is not null &&
                    !string.IsNullOrWhiteSpace(
                        previousYearToDateCell.Value))
                {
                    previousYearToDateAmount =
                        ParseDecimal(
                            previousYearToDateCell.Value,
                            disclosure.TracingNo,
                            "PreviousYearToDateAmount");
                }
            }
            else
            {
                string? previousYearPeriodPrefix =
                    GetPreviousYearPeriodPrefix(
                        periodCell.PeriodEndToDate);

                if (previousYearPeriodPrefix is not null)
                {
                    previousYearToDateAmount =
                        await dbContext.MonthlyActivitySummaries
                            .Where(x =>
                                x.Symbol == disclosure.Symbol &&
                                x.Rt == rt &&
                                x.PeriodEndDate.StartsWith(
                                    previousYearPeriodPrefix))
                            .OrderByDescending(
                                x => x.PublishDateTime)
                            .Select(
                                x => x.YearToDateAmount)
                            .FirstOrDefaultAsync(
                                cancellationToken);
                }
            }



            MonthlyActivitySummary? existingSummary =
                await dbContext.MonthlyActivitySummaries
                    .SingleOrDefaultAsync(
                        x =>
                            x.Symbol == disclosure.Symbol &&
                            x.PeriodEndDate ==
                            periodCell.PeriodEndToDate,
                        cancellationToken);

            if (existingSummary is null)
            {
                var summary =
                    new MonthlyActivitySummary(
                        symbol: disclosure.Symbol,
                        periodEndDate: periodCell.PeriodEndToDate,
                        yearEndDate: periodCell.YearEndToDate,
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
            else if (
                disclosure.PublishDateTime.HasValue &&
                (!existingSummary.PublishDateTime.HasValue ||
                 disclosure.PublishDateTime.Value >
                 existingSummary.PublishDateTime.Value))
            {

                existingSummary.Update(
                    yearEndDate: periodCell.YearEndToDate,
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
        catch
        {
            disclosure.SalesParseStatus =
                DisclosureParseStatus.Failed;

            disclosure.SalesParsedAt =
                DateTime.UtcNow;

            await dbContext.SaveChangesAsync(
                cancellationToken);

            throw;
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
    private static decimal ParseDecimal(string? value,
                                        long tracingNo,
                                        string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"{fieldName} is empty for disclosure {tracingNo}.");
        }

        string normalizedValue = value
            .Trim()
            .Replace(",", string.Empty, StringComparison.Ordinal)
            .Replace("٬", string.Empty, StringComparison.Ordinal)
            .Replace("،", string.Empty, StringComparison.Ordinal)
            .Replace('۰', '0')
            .Replace('۱', '1')
            .Replace('۲', '2')
            .Replace('۳', '3')
            .Replace('۴', '4')
            .Replace('۵', '5')
            .Replace('۶', '6')
            .Replace('۷', '7')
            .Replace('۸', '8')
            .Replace('۹', '9')
            .Replace('٠', '0')
            .Replace('١', '1')
            .Replace('٢', '2')
            .Replace('٣', '3')
            .Replace('٤', '4')
            .Replace('٥', '5')
            .Replace('٦', '6')
            .Replace('٧', '7')
            .Replace('٨', '8')
            .Replace('٩', '9');

        if (!decimal.TryParse(
                normalizedValue,
                NumberStyles.Number |
                NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out decimal result))
        {
            throw new InvalidOperationException(
                $"{fieldName} value '{value}' is invalid " +
                $"for disclosure {tracingNo}.");
        }

        return result;
    }
    private static string? GetPreviousYearPeriodPrefix(
    string? periodEndDate)
    {
        if (string.IsNullOrWhiteSpace(periodEndDate) ||
            periodEndDate.Length < 7 ||
            !int.TryParse(periodEndDate[..4], out int year))
        {
            return null;
        }

        return $"{year - 1:0000}{periodEndDate[4..7]}";
    }
}