using System.Globalization;

namespace FSH.Framework.Shared.Dates;

public static class PersianDateHelper
{
    private static readonly PersianCalendar Calendar = new();

    private static readonly CultureInfo Culture = CreateCulture();

    private static CultureInfo CreateCulture()
    {
        var culture = new CultureInfo("fa-IR");
        culture.DateTimeFormat.Calendar = Calendar;
        return culture;
    }


    /// <summary>
    /// تبدیل DateTime میلادی به رشته تاریخ شمسی
    /// مثال خروجی:
    /// 1405/05/01 19:52:41
    /// </summary>
    public static string ToPersian(DateTime value)
    {
        return value.ToString(
            "yyyy/MM/dd HH:mm:ss",
            Culture);
    }


    /// <summary>
    /// تبدیل رشته تاریخ شمسی کدال به DateTime میلادی
    /// مثال ورودی:
    /// ۱۴۰۵/۰۵/۰۱ ۱۹:۵۲:۴۱
    /// خروجی:
    /// 2026-07-23 19:52:41
    /// </summary>
    public static DateTime? ToGregorian(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        value = NormalizeDigits(value);

        var parts = value.Split(
            ['/', ' ', ':'],
            StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 5)
            return null;

        return Calendar.ToDateTime(
            int.Parse(parts[0]), // year
            int.Parse(parts[1]), // month
            int.Parse(parts[2]), // day
            int.Parse(parts[3]), // hour
            int.Parse(parts[4]), // minute
            parts.Length > 5 ? int.Parse(parts[5]) : 0, // second
            0);
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


#if false
/// Example Of Date Conversion 
    var date1 = PersianDateHelper.ToGregorian("۱۴۰۵/۰۵/۰۱ ۱۹:۵۲:۴۱");
    var date2 = PersianDateHelper.ToGregorian("1405/05/01 19:52:41");
    var date3 = PersianDateHelper.ToGregorian("1405/05/01 19:52");
    var original = new DateTime(2026, 7, 23, 19, 52, 41, DateTimeKind.Local);
    var persian1 = PersianDateHelper.ToPersian(original);

    var date5 = PersianDateHelper.ToPersian(DateTime.Now);
    var date6 = GregorianDateHelper.ToGregorian(DateTime.Now);
    var date7 = GregorianDateHelper.ToGregorian(DateTime.Now);
    var date8 = GregorianDateHelper.FromString("۲۰۲۶/۰۷/۲۳ ۱۹:۵۲:۴۱");
    var date9 = GregorianDateHelper.FromString("2026/07/23 19:52:41");
    var persian =
        PersianDateHelper.ToPersian(original);

    var converted =
        PersianDateHelper.ToGregorian(persian);
#endif
}