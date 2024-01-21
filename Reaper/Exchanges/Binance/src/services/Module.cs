using Microsoft.Extensions.DependencyInjection;
using Reaper.CommonLib.Interfaces;
using Reaper.Exchanges.Binance.Interfaces;

namespace Reaper.Exchanges.Binance.Services;
public static class Module
{
    public static IServiceCollection AddBinanceServices(this IServiceCollection services)
    {
        services.AddScoped<IBalanceService, BalanceService>();
        services.AddScoped<IBrokerService, BrokerService>();
        services.AddScoped<IMarketDataService, MarketDataService>();
        return services;
    }
}