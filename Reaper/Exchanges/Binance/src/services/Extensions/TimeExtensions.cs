
namespace Reaper.Exchanges.Binance.Services;
public static class TimeExtensions
{
    public static long ToUtcEpoch(this string dateStr)
    {
        var dateTime = DateTime.Parse(dateStr);
        // Convert to UTC
        var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(dateTime);

        // Convert to epoch time
        var epochTime = (long)(utcDateTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;

        return epochTime;
    }

    public static DateTime FromUtcEpoch(this long utcEpoch)
    {
        return DateTimeOffset.FromUnixTimeSeconds(utcEpoch).UtcDateTime;
    }
}