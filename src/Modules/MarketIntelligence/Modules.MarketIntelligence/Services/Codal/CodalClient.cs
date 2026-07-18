
using System.Net.Http.Json;
using FSH.Modules.MarketIntelligence.Contracts.Dtos;

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
        
        var url = $"https://search.codal.ir/api/search/v2/q?" +
          string.Join("&", parameters);
        var response = await _httpClient.GetFromJsonAsync<CodalSearchResponse>(
            url,
            cancellationToken);


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