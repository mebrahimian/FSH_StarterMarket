using FSH.Framework.Shared.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace FSH.Modules.MarketIntelligence.Services.MarketData;

public sealed class BorsMarketPriceProvider(
    IOptions<DatabaseOptions> databaseOptions)
    : IMarketPriceProvider
{
    public async Task<IReadOnlyDictionary<int, MarketPriceSnapshot>>
        GetLatestPricesAsync(
            IReadOnlyCollection<int> companyIds,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(companyIds);

        if (companyIds.Count == 0)
        {
            return new Dictionary<int, MarketPriceSnapshot>();
        }

        var result =
            new Dictionary<int, MarketPriceSnapshot>();

        await using var connection =
            new SqlConnection(
                databaseOptions.Value.ConnectionString);

        await connection.OpenAsync(
            cancellationToken);

        const string sql = """
    DECLARE @ids xml = @companyIds;

    SELECT
        p.CompanyId,
        p.Namad,
        p.[آخرين قيمت],
        p.[قيمت پاياني],
        p.ActDate
    FROM Bors.dbo.LastPriceList_View AS p
    INNER JOIN
    (
        SELECT
            x.value('.', 'int') AS CompanyId
        FROM @ids.nodes('/ids/id') AS t(x)
    ) AS requested
        ON requested.CompanyId = p.CompanyId;
    """;

        await using var command =
            new SqlCommand(
                sql,
                connection);

        string companyIdsXml =
            "<ids>" +
            string.Join(
                string.Empty,
                companyIds.Select(
                    companyId =>
                        $"<id>{companyId}</id>")) +
            "</ids>";

        command.Parameters.Add(
            new SqlParameter(
                "@companyIds",
                System.Data.SqlDbType.Xml)
            {
                Value = companyIdsXml
            });
        await using SqlDataReader reader =
            await command.ExecuteReaderAsync(
                cancellationToken);

        while (await reader.ReadAsync(
                   cancellationToken))
        {
            int companyId =
                reader.GetInt32(
                    reader.GetOrdinal("CompanyId"));

            int symbolOrdinal =
    reader.GetOrdinal("Namad");

            string? symbol =
                await reader.IsDBNullAsync(
                    symbolOrdinal,
                    cancellationToken)
                    ? null
                    : await reader.GetFieldValueAsync<string>(
                        symbolOrdinal,
                        cancellationToken);

            decimal? lastPrice =
                ReadDecimal(
                    reader,
                    "آخرين قيمت");

            decimal? closingPrice =
                ReadDecimal(
                    reader,
                    "قيمت پاياني");

            int tradeDateOrdinal = reader.GetOrdinal("ActDate");

            string? tradeDate =
                await reader.IsDBNullAsync(
                    tradeDateOrdinal,
                    cancellationToken)
                    ? null
                    : await reader.GetFieldValueAsync<string>(
                        tradeDateOrdinal,
                        cancellationToken);

            result[companyId] =
                new MarketPriceSnapshot(
                    CompanyId: companyId,
                    Symbol: symbol,
                    LastPrice: lastPrice,
                    ClosingPrice: closingPrice,
                    TradeDate: tradeDate);
        }

        return result;
    }

    private static decimal? ReadDecimal(
        SqlDataReader reader,
        string columnName)
    {
        int ordinal =
            reader.GetOrdinal(
                columnName);

        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        return Convert.ToDecimal(
            reader.GetValue(ordinal),
            System.Globalization.CultureInfo.InvariantCulture);
    }
}