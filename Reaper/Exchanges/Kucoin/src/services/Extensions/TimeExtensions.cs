using System.Globalization;

namespace Reaper.Exchanges.Kucoin.Services;
public static class TimeExtensions
{
    public static long ToUtcEpoch(this string dateStr)
    {
        var dateTime = DateTime.ParseExact(dateStr, "dd-MM-yyyy", CultureInfo.InvariantCulture);
        // Convert to UTC
        var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(dateTime);

        // Convert to epoch time
        var epochTime = (long)(utcDateTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;

        return epochTime;
    }

    public static DateTime FromUtcMillliSeconds(this long milliseconds)
    {
        return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime;
    }
}