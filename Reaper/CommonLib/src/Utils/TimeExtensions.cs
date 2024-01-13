namespace Reaper.CommonLib.Utils;
public static class TimeExtensions
{
    public static long ToUtcEpoch(this string localTime)
    {
        // Parse the local time
        var time = TimeSpan.Parse(localTime);

        // Get today's date in the local time zone
        var localDateTime = DateTime.Today.Add(time);

        // Convert to UTC
        var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(localDateTime);

        // Convert to epoch time
        var epochTime = (long)(utcDateTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;

        return epochTime;
    }

    public static DateTime FromUtcEpoch(this long utcEpoch)
    {
        return DateTimeOffset.FromUnixTimeSeconds(utcEpoch).UtcDateTime;
    }
}