using FSH.Modules.MarketIntelligence.Services.Codal.Configuration;
using Microsoft.Extensions.Options;
using System.Net;

namespace FSH.Modules.MarketIntelligence.Services.Codal.Portfolio;

public sealed class InvestmentPortfolioReader(
    HttpClient httpClient,
    IOptions<CodalOptions> options)
{
    internal async Task<IReadOnlyList<InvestmentPortfolioSheetData>> ReadAsync(
        string disclosureUrl,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            disclosureUrl);

        IReadOnlyList<PortfolioSheet> sheets = PortfolioSheetResolver.GetSheets(disclosureUrl);

        var result =
            new List<InvestmentPortfolioSheetData>(sheets.Count);

        foreach (PortfolioSheet sheet in sheets)
        {
            Uri reportUri = BuildReportUri(sheet.Url);
            string html =  await DownloadHtmlWithRetryAsync(reportUri, cancellationToken);
            IReadOnlyList<CodalTableRow> rows = CodalCellReader.ReadTableRows(html, sheet.MetaTableCode);
            CodalCellResult? metadata = CodalCellReader.FindCellValue(html, sheet.MetaTableCode, 1);
            result.Add(new InvestmentPortfolioSheetData(
                           SheetId: sheet.SheetId,
                           MetaTableCode: sheet.MetaTableCode,
                           IsListed: sheet.IsListed,
                           PeriodEndDate: metadata?.PeriodEndToDate,
                           Rows: rows));
        }

        return result;
    }

    private Uri BuildReportUri(
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
            new Uri(options.Value.BaseUrl),
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
                        "Codal rate limit/security verification triggered (HTTP 490).",
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
            catch (HttpRequestException ex)
            {
                lastException = ex;
            }
            catch (IOException ex)
            {
                lastException = ex;
            }
            catch (OperationCanceledException ex)
            {
                lastException = ex;
            }

            if (attempt < maxAttempts)
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(
                        attempt * 5),
                    cancellationToken);
            }
        }

        throw new HttpRequestException(
            $"Downloading Codal portfolio report failed after " +
            $"{maxAttempts} attempts. Url: {reportUri}",
            lastException);
    }
}

internal sealed record InvestmentPortfolioSheetData(
    int SheetId,
    int MetaTableCode,
    bool IsListed,
    string? PeriodEndDate,
    IReadOnlyList<CodalTableRow> Rows);