using Flurl.Http;
using Microsoft.Extensions.Options;
using Reaper.CommonLib.Interfaces;
using Reaper.Exchanges.Binance.Services.ApiModels;
using Reaper.Exchanges.Binance.Services.Configuration;

namespace Reaper.Exchanges.Binance.Services;
public class BalanceService(IOptions<BinanceOptions> options) : IBalanceService
{
    private readonly BinanceOptions _binanceOptions = options.Value;

    public async Task<TBalance?> GetBalanceAsync<TBalance>(string symbol, CancellationToken cancellationToken)
    where TBalance : class
    {
        using var flurlClient = CommonLib.Utils.FlurlExtensions.GetFlurlClient(_binanceOptions.BaseURL, false);
        var balanceFn = async (IFlurlClient client, object? requestData, CancellationToken cancellation) => 
            await client.Request()
                .AppendPathSegments("api", "v3", "account")
                .WithHeader("X-MBX-APIKEY", _binanceOptions.ApiKey)
                .WithSignedQueryParams(_binanceOptions.SecretKey, requestData)
                .GetAsync(HttpCompletionOption.ResponseContentRead, cancellation)
                .ReceiveJson<BalanceResponse>();
        Result<BalanceResponse> result = await balanceFn.CallAsync(flurlClient, null, cancellationToken);
        var balance = result.Data?.Balances.FirstOrDefault(b => b.Asset == symbol);
        return balance as TBalance;
    }
}