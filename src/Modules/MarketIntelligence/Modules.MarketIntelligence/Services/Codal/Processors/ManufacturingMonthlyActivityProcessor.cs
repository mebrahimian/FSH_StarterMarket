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

public sealed class ManufacturingMonthlyActivityProcessor(
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

        return disclosure.Let == 58
            && disclosure.Rt == 0
            && disclosure.SalesParseStatus == DisclosureParseStatus.Pending;
    }

    public async Task ProcessAsync(Disclosure disclosure, CancellationToken cancellationToken = default)
    {     
        var definitions = CodalDefinitionsProvider.Load();
        ArgumentNullException.ThrowIfNull(disclosure);

        if (!CanProcess(disclosure))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(disclosure.Url))
        {
            disclosure.SalesParseStatus = DisclosureParseStatus.Failed;

            disclosure.SalesParsedAt = DateTime.UtcNow;
            return;
        }

        try
        {
            Uri reportUri = BuildReportUri(disclosure.Url);

            string html = await DownloadHtmlWithRetryAsync(
                              reportUri,
                              cancellationToken);

            // از اینجا به بعد CodalCellFinder فعلی بدون تغییر استفاده می‌شود.
            CodalCellResult? MonthCell = CodalCellFinder.FindCellValue
                                 (
                                    html,
                                    definitions.ManufacturingMonthlySales.MetaTableId,
                                    definitions.ManufacturingMonthlySales.MetaTableCode,
                                    definitions.ManufacturingMonthlySales.SelectedCells["MonthlySales"]
                                 );
            CodalCellResult? YearToDateCell = CodalCellFinder.FindCellValue
                                 (
                                    html,
                                    definitions.ManufacturingMonthlySales.MetaTableId,
                                    definitions.ManufacturingMonthlySales.MetaTableCode,
                                    definitions.ManufacturingMonthlySales.SelectedCells["YearToDateSales"]
                                 );

            ////////
            ////////
            if (MonthCell is null || YearToDateCell is null)
            {
                throw new InvalidOperationException(
                    $"Sales cells were not found for disclosure {disclosure.TracingNo}.");
            }
            
            if (string.IsNullOrWhiteSpace(disclosure.Symbol))
            {
                throw new InvalidOperationException(
                    $"Disclosure {disclosure.TracingNo} does not have a symbol.");
            }
                        
            DateTime utcNow = DateTime.UtcNow;

            decimal monthlySalesAmount = ParseDecimal(MonthCell.Value,
                                                      disclosure.TracingNo,
                                                      "MonthlySalesAmount");

            decimal yearToDateSalesAmount = ParseDecimal(YearToDateCell.Value,
                                                         disclosure.TracingNo,
                                                         "YearToDateSalesAmount");

            MonthlySales? existingMonthlySales = 
                await dbContext.MonthlySales
                               .SingleOrDefaultAsync
                                  ( x => x.Symbol == disclosure.Symbol &&
                                    x.PeriodEndDate == MonthCell.PeriodEndToDate,
                                    cancellationToken
                                  );
            if (existingMonthlySales is null)
            {
                var monthlySales = new MonthlySales(
                    symbol: disclosure.Symbol,
                    periodEndDate: MonthCell.PeriodEndToDate!,
                    yearEndDate: MonthCell.YearEndToDate,

                    monthlySalesAmount: monthlySalesAmount,
                    yearToDateSalesAmount: yearToDateSalesAmount,

                    monthlySalesFormula: MonthCell.Formula,
                    monthlySalesAddress: MonthCell.Address,
                    monthlySalesRowSequence: MonthCell.RowSequence,

                    yearToDateSalesFormula: YearToDateCell.Formula,
                    yearToDateSalesAddress: YearToDateCell.Address,
                    yearToDateSalesRowSequence: YearToDateCell.RowSequence,

                    publishDateTime: disclosure.PublishDateTime,
                    disclosureId: disclosure.Id,
                    tracingNo: disclosure.TracingNo);

                await dbContext.MonthlySales.AddAsync(
                    monthlySales,
                    cancellationToken);
            }
            else
            {
                if (disclosure.PublishDateTime.HasValue &&
                    (!existingMonthlySales.PublishDateTime.HasValue || 
                    disclosure.PublishDateTime.Value > existingMonthlySales.PublishDateTime.Value)
                   )
                {
                    existingMonthlySales.Update(
                        yearEndDate: MonthCell.YearEndToDate,

                        monthlySalesAmount: monthlySalesAmount,
                        yearToDateSalesAmount: yearToDateSalesAmount,

                        monthlySalesFormula: MonthCell.Formula,
                        monthlySalesAddress: MonthCell.Address,
                        monthlySalesRowSequence: MonthCell.RowSequence,

                        yearToDateSalesFormula: YearToDateCell.Formula,
                        yearToDateSalesAddress: YearToDateCell.Address,
                        yearToDateSalesRowSequence: YearToDateCell.RowSequence,
                        publishDateTime: disclosure.PublishDateTime,
                        disclosureId: disclosure.Id,
                        tracingNo: disclosure.TracingNo);
                }
            }

            disclosure.SalesParseStatus = DisclosureParseStatus.Success;
            disclosure.SalesParsedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
           

        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            disclosure.SalesParseStatus = DisclosureParseStatus.Failed;

            disclosure.SalesParsedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
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
}