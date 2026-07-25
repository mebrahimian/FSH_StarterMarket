using System.Globalization;

namespace FSH.Framework.Shared.Dates;

public static class GregorianDateHelper
{
    private static readonly CultureInfo Culture =
        CultureInfo.InvariantCulture;


    /// <summary>
    /// DateTime میلادی -> رشته میلادی
    /// مثال خروجی:
    /// 2026/07/24 19:52:41
    /// </summary>
    public static string ToGregorian(DateTime value)
    {
        return value.ToString(
            "yyyy/MM/dd HH:mm:ss",
            Culture);
    }


    public static string ToGregorian(DateTime? value)
    {
        return value is null
            ? string.Empty
            : ToGregorian(value.Value);
    }


    /// <summary>
    /// رشته تاریخ میلادی -> DateTime
    /// مثال ورودی:
    /// ۲۰۲۶/۰۷/۲۴ ۱۹:۵۲:۴۱
    /// </summary>
    public static DateTime? FromString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        value = NormalizeDigits(value);

        if (DateTime.TryParseExact(
            value,
            "yyyy/MM/dd HH:mm:ss",
            Culture,
            DateTimeStyles.None,
            out var result))
        {
            return result;
        }

        return null;
    }


    private static string NormalizeDigits(string value)
    {
        return value
            .Replace('۰', '0')
            .Replace('۱', '1')
            .Replace('۲', '2')
            .Replace('۳', '3')
            .Replace('۴', '4')
            .Replace('۵', '5')
            .Replace('۶', '6')
            .Replace('۷', '7')
            .Replace('۸', '8')
            .Replace('۹', '9');
    }
}