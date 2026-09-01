namespace FSH.Modules.MarketIntelligence.Utilities;

internal static class PersianTextNormalizer
{
    public static string NormalizeDigits(
        string value)
    {
        ArgumentNullException.ThrowIfNull(
            value);

        char[] characters =
            value.ToCharArray();

        for (
            int index = 0;
            index < characters.Length;
            index++)
        {
            char character =
                characters[index];

            if (
                character is >= '۰' and <= '۹')
            {
                characters[index] =
                    (char)(
                        '0' +
                        character -
                        '۰');
            }
            else if (
                character is >= '٠' and <= '٩')
            {
                characters[index] =
                    (char)(
                        '0' +
                        character -
                        '٠');
            }
        }

        return new string(characters);
    }

    public static string NormalizeNumber(
        string value)
    {
        return NormalizeDigits(value)
            .Trim()
            .Replace(
                ",",
                string.Empty,
                StringComparison.Ordinal)
            .Replace(
                "٬",
                string.Empty,
                StringComparison.Ordinal)
            .Replace(
                "،",
                string.Empty,
                StringComparison.Ordinal);
    }
    public static string NormalizeText(
    string value)
    {
        ArgumentNullException.ThrowIfNull(
            value);

        return NormalizeDigits(value)
            .Replace('ي', 'ی')
            .Replace('ى', 'ی')
            .Replace('ك', 'ک');
    }
}