namespace FSH.Framework.BuildingBlocks.Shared.Globalization;

public static class PersianTextNormalizer
{
    public static string Normalize(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return value
            .Trim()
            .Replace('ك', 'ک')
            .Replace('ي', 'ی')
            .Replace('ى', 'ی');
    }

    public static string NormalizeForMatch(string value)
    {
        return Normalize(value)
            .Replace('أ', 'ا').Replace('إ', 'ا')
            .Replace('۰', '0').Replace('۱', '1').Replace('۲', '2')
            .Replace('۳', '3').Replace('۴', '4').Replace('۵', '5')
            .Replace('۶', '6').Replace('۷', '7').Replace('۸', '8')
            .Replace('۹', '9').Replace('٠', '0').Replace('١', '1')
            .Replace('٢', '2').Replace('٣', '3').Replace('٤', '4')
            .Replace('٥', '5').Replace('٦', '6').Replace('٧', '7')
            .Replace('٨', '8').Replace('٩', '9').Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("\u200C", string.Empty, StringComparison.Ordinal)
            .Replace("\u200D", string.Empty, StringComparison.Ordinal);
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
}