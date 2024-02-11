using Reaper.CommonLib.Interfaces;
using Reaper.Exchanges.Kucoin.Interfaces;
using Reaper.Exchanges.Kucoin.Services;
using Reaper.Exchanges.Kucoin.Services.Models;

namespace Reaper.Exchanges.Kucoin.Api;
public static class Dependencies
{
    public static Serilog.Core.Logger GetLogger()
    {
        DirectoryInfo currentDirectory = new(Directory.GetCurrentDirectory());
        return CommonLib.Utils.RLogger.GetLogger(
            httpLogsPath: Path.Combine(currentDirectory.FullName, "logs/httpLogs.txt"),
            appLogsPath: Path.Combine(currentDirectory.FullName, "logs/appLogs.txt"));
    }

    public static void AddReaperServices(this IServiceCollection services)
    {
        var configuration = new ConfigurationBuilder()
        //todo: remove setbase path( it is for notebook)
        // .SetBasePath("C:\\Users\\tyueksel\\Desktop\\Reaper_\\reaper\\Reaper\\Exchanges\\Kucoin\\src\\api")
        .AddJsonFile("appsettings.json")

        .AddUserSecrets<Program>()
        .Build();

        services.Configure<KucoinOptions>(configuration.GetSection("Kucoin"));
        services.AddScoped<IBrokerService, BrokerService>();
        services.AddScoped<IBalanceService, BalanceService>();
        services.AddScoped<IMarketDataService, FuturesMarketDataService>();
        services.AddScoped<IBackTestService, BackTestService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IPositionInfoService, PositionInfoService>();
        services.AddScoped<IRunner, Runner>();
        services.AddScoped<IFuturesHub, FuturesHub>();
        services.AddScoped<IBreakout, Breakout>();
    }
}
