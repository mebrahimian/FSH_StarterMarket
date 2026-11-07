
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace FSH.Modules.MarketIntelligence.Services.Codal.Readers;

public sealed record CapitalIncreaseRegistrationData(
    string AssemblyDateRaw,
    string RegistrationDateRaw,
    decimal PreviousCapital,
    decimal NewCapital,
    decimal? CashIncoming,
    decimal? RetainedEarnings,
    decimal? Reserves,
    decimal? SharePremium,
    decimal? RevaluationSurplus);

public static class CapitalIncreaseRegistrationReader
{
    public static CapitalIncreaseRegistrationData Read(string html)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(html);

        string assemblyDate =
            RequiredText(html, "lblLastExtraAssembly");

        string registrationDate =
            RequiredText(html, "txbDate");

        decimal previousCapital =
            RequiredAmount(html, "lblPreviousCapital");

        decimal newCapital =
            RequiredAmount(html, "lblNewCapital");

        if (previousCapital <= 0 || newCapital <= previousCapital)
        {
            throw new FormatException(
                "Invalid capital increase amounts.");
        }

        return new CapitalIncreaseRegistrationData(
            assemblyDate,
            registrationDate,
            previousCapital,
            newCapital,
            OptionalAmount(html, "txbCashIncoming"),
            OptionalAmount(html, "txbRetaindedEarning"),
            OptionalAmount(html, "txbReserves"),
            OptionalAmount(html, "txbSarfSaham"),
            OptionalAmount(html, "txbRevaluationSurplus"));
    }

    private static string RequiredText(string html, string id)
    {
        return ReadText(html, id)
            ?? throw new FormatException(
                $"Required Codal field '{id}' was not found.");
    }

    private static decimal RequiredAmount(string html, string id)
    {
        return OptionalAmount(html, id)
            ?? throw new FormatException(
                $"Required Codal amount '{id}' was not found.");
    }

    private static decimal? OptionalAmount(string html, string id)
    {
        string? value = ReadText(html, id);

        if (value is null)
        {
            return null;
        }

        string normalized = value
            .Replace(",", "", StringComparison.Ordinal)
            .Replace("٬", "", StringComparison.Ordinal)
            .Replace(" ", "", StringComparison.Ordinal)
            .Replace("\u00A0", "", StringComparison.Ordinal);

        if (!decimal.TryParse(
            normalized,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out decimal amount))
        {
            throw new FormatException(
                $"Invalid value for '{id}': {value}");
        }

        return amount;
    }

    private static string? ReadText(string html, string id)
    {
        string escapedId = Regex.Escape(id);

        string spanPattern =
            $@"<span\b(?=[^>]*\bid\s*=\s*[""']{escapedId}[""'])[^>]*>(?<value>.*?)</span>";

        Match match = Regex.Match(
            html,
            spanPattern,
            RegexOptions.IgnoreCase |
            RegexOptions.Singleline |
            RegexOptions.CultureInvariant);

        if (match.Success)
        {
            return CleanText(match.Groups["value"].Value);
        }

        string inputPattern =
            $@"<input\b(?=[^>]*\bid\s*=\s*[""']{escapedId}[""'])[^>]*>";

        match = Regex.Match(
            html,
            inputPattern,
            RegexOptions.IgnoreCase |
            RegexOptions.Singleline |
            RegexOptions.CultureInvariant);

        if (!match.Success)
        {
            return null;
        }

        Match valueMatch = Regex.Match(
            match.Value,
            @"\bvalue\s*=\s*[""'](?<value>[^""']*)[""']",
            RegexOptions.IgnoreCase |
            RegexOptions.CultureInvariant);

        return valueMatch.Success
            ? CleanText(valueMatch.Groups["value"].Value)
            : null;
    }

    private static string? CleanText(string raw)
    {
        string withoutTags =
            Regex.Replace(raw, "<[^>]+>", "");

        string text =
            WebUtility.HtmlDecode(withoutTags).Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return NormalizeDigits(text);
    }

    private static string NormalizeDigits(string value)
    {
        return string.Create(
            value.Length,
            value,
            static (destination, source) =>
            {
                for (int i = 0; i < source.Length; i++)
                {
                    char c = source[i];

                    destination[i] = c switch
                    {
                        >= '۰' and <= '۹' =>
                            (char)('0' + c - '۰'),

                        >= '٠' and <= '٩' =>
                            (char)('0' + c - '٠'),

                        _ => c
                    };
                }
            });
    }
}
