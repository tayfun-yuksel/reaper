using Serilog;

namespace Reaper.CommonLib.Utils;
public static class RLogger
{
    public static Serilog.Core.Logger GetLogger(string httpLogsPath, string appLogsPath)
    {
        return new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(e => e.Properties.ContainsKey("http")) 
                .WriteTo.File(httpLogsPath, rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"))
            .WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(e => e.Properties.ContainsKey("app")) 
                .WriteTo.File(appLogsPath, rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception} HttpRequestId: {HttpRequestId}"))
            .CreateLogger();
    }
}