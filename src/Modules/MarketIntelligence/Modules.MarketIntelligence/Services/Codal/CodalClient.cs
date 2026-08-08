
using System.Net.Http.Json;
using FSH.Modules.MarketIntelligence.Contracts.Dtos;
using FSH.Modules.MarketIntelligence.Services.Codal.Interfaces;

namespace FSH.Modules.MarketIntelligence.Services.Codal;

public sealed class CodalClient : ICodalClient
{
    private readonly HttpClient _httpClient;

    public CodalClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CodalSearchResponse> SearchAsync(
        CodalSearchRequest request,
        CancellationToken cancellationToken = default)
    {

        ArgumentNullException.ThrowIfNull(request);
        var parameters = new List<string>();

        AddParameter("Audited", request.Audited);
        AddParameter("AuditorRef", request.AuditorRef);
        AddParameter("Category", request.Category);
        AddParameter("FromDate", request.FromDate);
        AddParameter("ToDate", request.ToDate);
        AddParameter("Childs", request.Childs);
        AddParameter("CompanyState", request.CompanyState);
        AddParameter("CompanyType", request.CompanyType);
        AddParameter("Consolidatable", request.Consolidatable);
        AddParameter("IsNotAudited", request.IsNotAudited);
        AddParameter("Length", request.Length);
        AddParameter("LetterType", request.LetterType);
        AddParameter("Mains", request.Mains);
        AddParameter("NotAudited", request.NotAudited);
        AddParameter("NotConsolidatable", request.NotConsolidatable);
        AddParameter("PageNumber", request.PageNumber);
        AddParameter("Publisher", request.Publisher);
        AddParameter("ReportingType", request.ReportingType);
        AddParameter("TracingNo", request.TracingNo);
        AddParameter("search", true);
        AddParameter("symbol", request.Symbol);
        
        var url = $"https://search.codal.ir/api/search/v2/q?" +
          string.Join("&", parameters);

        CodalSearchResponse? response = null;
        const int maxRetries = 3;
        for (int retry = 1; retry <= maxRetries; retry++)
        {
            try
            {
                response = await _httpClient.GetFromJsonAsync<CodalSearchResponse>(
                    url,
                    cancellationToken);

                break;
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine(
                    $"Codal timeout. Attempt {retry}/{maxRetries}");

                if (retry < maxRetries)
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(retry * 5),
                        cancellationToken);
                }
                else
                {
                    throw;
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine(
                    $"Codal request failed. Attempt {retry}/{maxRetries}: {ex.Message}");

                if (retry < maxRetries)
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(retry * 5),
                        cancellationToken);
                }
                else
                {
                    throw;
                }
            }
        }

        return response ?? new CodalSearchResponse();

        void AddParameter(string name, object? value)
        {
            if (value is null)
                return;

            var text = value.ToString();

            if (string.IsNullOrWhiteSpace(text))
                return;

            parameters.Add($"{name}={Uri.EscapeDataString(text)}");
        }
    }
}