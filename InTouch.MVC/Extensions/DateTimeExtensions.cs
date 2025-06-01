namespace InTouch.MVC.Extensions;

public static class DateTimeExtensions
{
    /// <summary>
    /// Converts a UTC DateTime to the user's timezone if available, otherwise to application default (GMT+2)
    /// </summary>
    public static DateTime ToUserTimeZone(this DateTime utcDateTime, HttpContext httpContext = null)
    {
        string userTimeZoneId = null;
        if (httpContext?.Request.Cookies.TryGetValue("user_timezone", out userTimeZoneId) == true)
        {
            try
            {
                // This works with IANA timezone IDs directly
                TimeZoneInfo tzInfo = TimeZoneConverter.TZConvert.GetTimeZoneInfo(userTimeZoneId);
                return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, tzInfo);
            }
            catch
            {
                // Fall back to default
            }
        }

        // Default to GMT+2 using IANA name
        try
        {
            TimeZoneInfo tzInfo = TimeZoneConverter.TZConvert.GetTimeZoneInfo("Europe/Warsaw");
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, tzInfo);
        }
        catch
        {
            return utcDateTime.AddHours(2); // Ultimate fallback
        }
    }

    /// <summary>
    /// Returns a formatted string representation of the DateTime in the user's timezone
    /// </summary>
    public static string ToUserTimeString(this DateTime utcDateTime, string format = "dd MMM yyyy HH:mm", HttpContext httpContext = null)
    {
        return ToUserTimeZone(utcDateTime, httpContext).ToString(format);
    }

    // Format-first overload for convenience in views
    public static string ToLocalTimeString(this DateTime utcDateTime, string format)
    {
        return ToUserTimeZone(utcDateTime).ToString(format);
    }

    public static string ToLocalTimeString(this DateTime utcDateTime, string format, string cultureName)
    {
        var culture = System.Globalization.CultureInfo.GetCultureInfo(cultureName);
        return ToUserTimeZone(utcDateTime).ToString(format, culture);
    }

    // Keep the original method with default format
    public static string ToLocalTimeString(this DateTime utcDateTime)
    {
        return ToUserTimeZone(utcDateTime).ToString("dd MMM yyyy HH:mm");
    }

    // Keep the original method
    public static DateTime ToLocalTimeZone(this DateTime utcDateTime) => ToUserTimeZone(utcDateTime);
}