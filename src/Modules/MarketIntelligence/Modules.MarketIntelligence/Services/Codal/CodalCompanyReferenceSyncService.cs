using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Domain;
using FSH.Modules.MarketIntelligence.Services.Codal.Contracts;
using FSH.Modules.MarketIntelligence.Services.Tsetmc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Modules.MarketIntelligence.Domain;
using SendGrid.Helpers.Mail;
using System.Globalization;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;


namespace FSH.Modules.MarketIntelligence.Services.Codal;

public sealed class CodalCompanyReferenceSyncService(
    HttpClient httpClient,
    MarketIntelligenceDbContext dbContext,
    ILogger<CodalCompanyReferenceSyncService> logger,
    IOptions<TsetmcOptions> tsetmcOptions)
{
    private readonly TsetmcOptions _tsetmcOptions = tsetmcOptions.Value;
    //  private static readonly Uri IndustriesUrl = new("https://search.codal.ir/api/search/v1/IndustryGroup");
    private static readonly Uri CompaniesUrl = new("https://search.codal.ir/api/search/v1/companies");

    public async Task SyncAsync(
        CancellationToken cancellationToken)
    {
        await RefreshIndustriesAsync(cancellationToken).ConfigureAwait(false); // Update Industrie                                                                   
        await RefreshInstrumentTypesAsync(cancellationToken).ConfigureAwait(false); // Update YVal                                                                   
        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                CompaniesUrl);

        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue(
                "application/json"));

        using HttpResponseMessage response =
            await httpClient
                .SendAsync(
                    request,
                    cancellationToken)
                .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        string json =
            await response.Content
                .ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

        var companies =
            JsonSerializer.Deserialize<List<CodalCompanyDto>>(json);

        if (companies is null ||
            companies.Count == 0)
        {
            throw new InvalidOperationException(
                "Codal companies API returned no data.");
        }

        var imports =
            companies
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.Symbol) &&
                    !string.IsNullOrWhiteSpace(x.Isic) &&
                    x.IndustryGroupCode is >= 0)
                .Select(x =>
                {
                    int industryGroupCode =
                        x.IndustryGroupCode
                            ?? throw new InvalidOperationException(
                                "IndustryGroupCode is required.");

                    string symbol =
                        x.Symbol!.Trim();

                    string isic =
                        x.Isic!.Trim();

                    string industryId =
                        industryGroupCode
                            .ToString(
                                CultureInfo.InvariantCulture);

                    return new CodalCompanyImport
                    {
                        Symbol = symbol,
                        CompanyName = x.Name?.Trim(),
                        IndustryId = industryId,
                        Isic = isic,
                        IndustryGroupId =
                            isic.Length >= 3
                                ? isic[..3]
                                : isic,
                        ReportingType =
                            x.ReportingType,
                        State =
                            x.State,
                        CompanyType =
                            x.Type
                    };
                })
                .ToList();

        if (imports.Count == 0)
        {
            throw new InvalidOperationException(
                "Codal companies API returned no valid ISIC data.");
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Codal companies received: {ReceivedCount}, valid imports: {ImportCount}",
                companies.Count,
                imports.Count);
        }

        var strategy =
    dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(
            async () =>
            {
                await using var transaction =
                    await dbContext.Database
                        .BeginTransactionAsync(
                            cancellationToken)
                        .ConfigureAwait(false);

                try
                {
                    await dbContext.Database
                        .ExecuteSqlRawAsync(
                            "TRUNCATE TABLE dbo.CodalCompanyImport;",
                            cancellationToken)
                        .ConfigureAwait(false);

                    dbContext.CodalCompanyImports
                        .AddRange(imports);

                    await dbContext
                        .SaveChangesAsync(cancellationToken)
                        .ConfigureAwait(false);

                    int updatedRows =
                        await UpdateCompanyIndustriesAsync(
                            cancellationToken)
                            .ConfigureAwait(false);

                    int insertedRows =
                        await InsertCompanyIndustriesAsync(
                            cancellationToken)
                            .ConfigureAwait(false);

                    await transaction
                        .CommitAsync(cancellationToken)
                        .ConfigureAwait(false);

                    if (logger.IsEnabled(LogLevel.Information))
                    {
                        logger.LogInformation(
                            "Codal company reference sync completed. Imported={Imported}, Updated={Updated}, Inserted={Inserted}",
                            imports.Count,
                            updatedRows,
                            insertedRows);
                    }
                }
                catch
                {
                    await transaction
                        .RollbackAsync(cancellationToken)
                        .ConfigureAwait(false);

                    throw;
                }
            });
    }

    private async Task<int> UpdateCompanyIndustriesAsync(
        CancellationToken cancellationToken)
    {
        const string sql = """
            ;WITH Matched AS
            (
                SELECT
                    M.CompanyId,
                    C.IndustryId,
                    C.IndustryGroupId,
                    C.Isic
                FROM dbo.CodalCompanyImport C
                INNER JOIN marketintelligence.vw_CompanyMaster M
                    ON
                    LTRIM(RTRIM(
                        REPLACE(
                            REPLACE(
                                REPLACE(
                                    M.Symbol,
                                    N'ي',
                                    N'ی'
                                ),
                                N'ك',
                                N'ک'
                            ),
                            NCHAR(8204),
                            N' '
                        )
                    ))
                    =
                    LTRIM(RTRIM(
                        REPLACE(
                            REPLACE(
                                REPLACE(
                                    C.Symbol,
                                    N'ي',
                                    N'ی'
                                ),
                                N'ك',
                                N'ک'
                            ),
                            NCHAR(8204),
                            N' '
                        )
                    ))
                INNER JOIN dbo.Industries I
                    ON I.IndustryId = C.IndustryId
                WHERE   M.IsListed = 1 AND
                        LTRIM(RTRIM(C.Symbol)) NOT IN ( N'و دانا' )
            )
            UPDATE CI
            SET
                CI.IndustryId = M.IndustryId,
                CI.IndustryGroupId = M.IndustryGroupId,
                CI.Isic = M.Isic
            FROM marketintelligence.CompanyIndustries CI
            INNER JOIN Matched M
                ON M.CompanyId = CI.CompanyId
            WHERE
                   ISNULL(CI.IndustryId, '') <> ISNULL(M.IndustryId, '')
                OR ISNULL(CI.IndustryGroupId, '') <> ISNULL(M.IndustryGroupId, '')
                OR ISNULL(CI.Isic, '') <> ISNULL(M.Isic, '');
            """;

        return await dbContext.Database
            .ExecuteSqlRawAsync(
                sql,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<int> InsertCompanyIndustriesAsync(
        CancellationToken cancellationToken)
    {
        const string sql = """
            ;WITH Matched AS
            (
                SELECT
                    M.CompanyId,
                    C.IndustryId,
                    C.IndustryGroupId,
                    C.Isic
                FROM dbo.CodalCompanyImport C
                INNER JOIN marketintelligence.vw_CompanyMaster M
                    ON
                    LTRIM(RTRIM(
                        REPLACE(
                            REPLACE(
                                REPLACE(
                                    M.Symbol,
                                    N'ي',
                                    N'ی'
                                ),
                                N'ك',
                                N'ک'
                            ),
                            NCHAR(8204),
                            N' '
                        )
                    ))
                    =
                    LTRIM(RTRIM(
                        REPLACE(
                            REPLACE(
                                REPLACE(
                                    C.Symbol,
                                    N'ي',
                                    N'ی'
                                ),
                                N'ك',
                                N'ک'
                            ),
                            NCHAR(8204),
                            N' '
                        )
                    ))
                INNER JOIN dbo.Industries I
                    ON I.IndustryId = C.IndustryId
                WHERE
                    M.IsListed = 1
                    AND LTRIM(RTRIM(C.Symbol)) <> N'و دانا'
            )
            INSERT INTO marketintelligence.CompanyIndustries
            (
                CompanyId,
                IndustryId,
                IndustryGroupId,
                Isic
            )
            SELECT
                M.CompanyId,
                M.IndustryId,
                M.IndustryGroupId,
                M.Isic
            FROM Matched M
            WHERE NOT EXISTS
            (
                SELECT 1
                FROM marketintelligence.CompanyIndustries CI
                WHERE CI.CompanyId = M.CompanyId
            );
            """;

        return await dbContext.Database
            .ExecuteSqlRawAsync(
                sql,
                cancellationToken)
            .ConfigureAwait(false);
    }
    private async Task RefreshIndustriesAsync(
    CancellationToken cancellationToken)
    {
        var industriesUrl =
            new Uri($"https://search.codal.ir/api/search/v1/IndustryGroup");

        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                industriesUrl);

        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue(
                "application/json"));

        using HttpResponseMessage response =
            await httpClient
                .SendAsync(
                    request,
                    cancellationToken)
                .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        string json =
            await response.Content
                .ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

        var industries =
            JsonSerializer.Deserialize<List<CodalIndustryDto>>(json);

        if (industries is null || industries.Count == 0)
        {
            throw new InvalidOperationException(
                "Codal industries API returned no valid data.");
        }

        var existingIndustryList = await dbContext.Industries
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var existingIndustries = existingIndustryList.ToDictionary(
                x => x.IndustryId.Trim(),
                StringComparer.Ordinal);

        int inserted = 0;
        int updated = 0;

        foreach (var source in industries)
        {
            if (string.IsNullOrWhiteSpace(source.Name))
            {
                continue;
            }

            string industryId =
                source.Id.ToString(
                    CultureInfo.InvariantCulture);

            string industryName =
                source.Name.Trim();

            if (!existingIndustries.TryGetValue(
                    industryId,
                    out var existing))
            {
                dbContext.Industries.Add(
                    new Industry
                    {
                        IndustryId = industryId,
                        IndustryName = industryName
                    });

                inserted++;
                continue;
            }

            if (!string.Equals(
                    existing.IndustryName,
                    industryName,
                    StringComparison.Ordinal))
            {
                existing.IndustryName = industryName;
                updated++;
            }
        }

        await dbContext
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Codal industries sync completed. Received={Received}, Inserted={Inserted}, Updated={Updated}",
                industries.Count,
                inserted,
                updated);
        }
    }
    private async Task RefreshInstrumentTypesAsync(
    CancellationToken cancellationToken)
    {
        var marketWatchUri =
            new Uri(
                _tsetmcOptions.MarketWatchUrl,
                UriKind.Absolute);

        byte[] bytes = await httpClient
    .GetByteArrayAsync(
        marketWatchUri,
        cancellationToken)
    .ConfigureAwait(false);

        await using var compressedStream =
            new MemoryStream(bytes);

        await using var gzipStream =
            new GZipStream(
                compressedStream,
                CompressionMode.Decompress);

        using var reader =
            new StreamReader(
                gzipStream,
                Encoding.UTF8);

        string content =
            await reader
                .ReadToEndAsync(cancellationToken)
                .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException(
                "TSETMC MarketWatch returned no data.");
        }

        var instruments = ParseMarketWatch(content);
        await UpsertTsetmcInstrumentsAsync(instruments, cancellationToken).ConfigureAwait(false);


        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "TSETMC instruments parsed: {Count}",
                instruments.Count);
        }
    }
    private async Task UpsertTsetmcInstrumentsAsync(
    IReadOnlyCollection<TsetmcInstrumentDto> instruments,
    CancellationToken cancellationToken)
    {
        var existing = await dbContext.TsetmcInstruments
            .ToDictionaryAsync(
                x => x.InsCode,
                cancellationToken)
            .ConfigureAwait(false);

        DateTimeOffset now = DateTimeOffset.UtcNow;

        foreach (TsetmcInstrumentDto item in instruments)
        {
            if (existing.TryGetValue(
                item.InsCode,
                out TsetmcInstrument? entity))
            {
                entity.Isin = item.Isin;
                entity.Symbol = item.Symbol;
                entity.Name = item.Name;
                entity.YVal = item.YVal;
                entity.LastSeenAt = now;

                continue;
            }

            entity = new TsetmcInstrument
            {
                InsCode = item.InsCode,
                Isin = item.Isin,
                Symbol = item.Symbol,
                Name = item.Name,
                YVal = item.YVal,
                LastSeenAt = now
            };

            dbContext.TsetmcInstruments.Add(entity);

            existing.Add(
                entity.InsCode,
                entity);
        }

        await dbContext
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }
    private static List<TsetmcInstrumentDto> ParseMarketWatch(string content)
    {
        var result = new List<TsetmcInstrumentDto>();

        string[] rows = content.Split(
            ';',
            StringSplitOptions.RemoveEmptyEntries);

        foreach (string row in rows)
        {
            string[] parts = row.Split(',');

            if (parts.Length < 23)
            {
                continue;
            }

            result.Add(
                new TsetmcInstrumentDto(
                    InsCode: parts[0].Trim(),
                    Isin: parts[1].Trim(),
                    Symbol: parts[2].Trim(),
                    Name: parts[3].Trim(),
                    YVal: parts[22].Trim()));
        }

        return result;
    }
    private sealed record TsetmcInstrumentDto(
    string InsCode,
    string Isin,
    string Symbol,
    string Name,
    string YVal);
}