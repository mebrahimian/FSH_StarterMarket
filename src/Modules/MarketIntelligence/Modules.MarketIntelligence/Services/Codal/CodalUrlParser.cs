using System;
using System.Collections.Generic;
using System.Text;

namespace FSH.Modules.MarketIntelligence.Services.Codal;

public static class CodalUrlParser
{
#pragma warning disable S1075
    private const string CodalBaseUrl = "https://www.codal.ir";
#pragma warning restore S1075
    public static CodalUrlInfo Parse(string? url)
    {

        if (string.IsNullOrWhiteSpace(url))
            return new(null, null, null, null);


        var uri = new Uri($"{CodalBaseUrl}{url}");

        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);

        return new CodalUrlInfo(
            ParseShort(query["let"]),
            ParseByte(query["rt"]),
            ParseByte(query["ct"]),
            ParseShort(query["ft"]));
    }

    private static short? ParseShort(string? value) =>
        short.TryParse(value, out var result)
            ? result
            : null;

    private static byte? ParseByte(string? value) =>
        byte.TryParse(value, out var result)
            ? result
            : null;
}
