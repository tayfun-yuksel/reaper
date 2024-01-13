
using Reaper.CommonLib.Interfaces;
using Reaper.Exchanges.Binance.Services;

namespace Reaper.Exchanges.Binance.Api;
public static class Module
{
    public static IServiceCollection AddBinanceServices(this IServiceCollection services)
    {
        services.AddScoped<IBalanceService, BalanceService>();
        return services;
    }
}