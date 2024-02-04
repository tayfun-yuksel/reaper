using Serilog;

namespace Reaper.Exchanges.Kucoin.Services;
public static class RLogger
{
    public static Serilog.ILogger HttpLog => Log.ForContext("http", string.Empty);
    public static Serilog.ILogger AppLog => Log.ForContext("app", string.Empty);
}