namespace FSH.Framework.Shared.Utilities;

public static class FSort
{
    public static string Normalize(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return value
            .Replace('ي', 'ی')
            .Replace('ى', 'ی')
            .Replace('ك', 'ک')
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("\u200C", string.Empty, StringComparison.Ordinal)
            .Replace("\u200D", string.Empty, StringComparison.Ordinal);
    }
}