using System.Globalization;

namespace Reaper.Exchanges.Kucoin.Services;
public static class TimeExtensions
{
    public static readonly List<int> AllowedIntervalInMinutes = [1, 3, 5, 15, 30, 60, 120, 240, 480, 720, 1440, 10080];

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

    public static DateTime FromUtcMsToLocalTime(this long milliseconds)
    {
        var res = DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).DateTime.ToLocalTime();
        return res;
    }
}