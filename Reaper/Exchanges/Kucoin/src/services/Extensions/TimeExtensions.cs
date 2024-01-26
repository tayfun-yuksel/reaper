using System.Globalization;

namespace Reaper.Exchanges.Kucoin.Services;
public static class TimeExtensions
{
    public static long ToUtcEpochMs(this string dateStr)
    {
        string[] formats = ["dd-MM-yyyy HH:mm", "dd-MM-yyyy"]; 

        if (DateTime.TryParseExact(dateStr.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
        {
            Console.WriteLine("Parsed date: " + parsedDate.ToString("dd-MM-yyyy HH:mm"));
        }
        else
        {
            Console.WriteLine("Unable to parse date: " + dateStr);
        }

        var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(parsedDate);
        var epochTime = (long)(utcDateTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
            .TotalMilliseconds;

        return epochTime;
    }

    public static DateTime FromUtcMs(this long milliseconds)
    {
        return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime;
    }

    public static string GetStartTimeFromInterval(int interval)
    {
        var allowedIntervalInMinutes = new List<int> { 1, 3, 5, 15, 30, 60, 120, 240, 360, 720, 1440, 10080};
        if (!allowedIntervalInMinutes.Contains(interval))
        {
            throw new ArgumentException("Interval must be one of the following: " + string.Join(", ", allowedIntervalInMinutes));
        }
        return string.Empty;
    }
}