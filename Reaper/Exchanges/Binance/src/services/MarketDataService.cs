
using System.Globalization;
using Flurl.Http;
using Microsoft.Extensions.Options;
using Reaper.Exchanges.Binance.Interfaces;
using Reaper.Exchanges.Binance.Services.ApiModels;
using Reaper.Exchanges.Binance.Services.Configuration;
using Reaper.Exchanges.Binance.Services.Converters;

namespace Reaper.Exchanges.Binance.Services;
public class MarketDataService(IOptions<BinanceOptions> options) : IMarketDataService
{
    private readonly BinanceOptions _binanceOptions = options.Value;

    public async Task<SymbolExchangeInfoResponse> GetSymbolExchangeInfoAsync(string symbol, CancellationToken cancellationToken)
    {
        using var flurlClient = FlurlExtensions.GetFlurlClient(_binanceOptions);
        var symbolExchangeInfoFn = async (IFlurlClient client, object? requestData, CancellationToken cancellation) => 
            await client.Request()
                .AppendPathSegments("api", "v3", "exchangeInfo")
                .SetQueryParam("symbol", symbol)
                .GetAsync(HttpCompletionOption.ResponseContentRead, cancellation)
                .ReceiveJson<SymbolExchangeInfoResponse>();

        Result<SymbolExchangeInfoResponse> result = await symbolExchangeInfoFn.CallAsync(flurlClient, null, cancellationToken);
        if (result.Error != null)
        {
            return  new();
        }
        return result.Data!;
    }

    public async Task<decimal> GetSymbolPriceAsync(string symbol, CancellationToken cancellationToken)
    {
        using var flurlClient = FlurlExtensions.GetFlurlClient(_binanceOptions);
        var symbolPriceFn = async (IFlurlClient client, object? requestData, CancellationToken cancellation) => 
            await client.Request()
                .AppendPathSegments("api", "v3", "ticker", "price")
                .SetQueryParam("symbol", symbol)
                .GetAsync(HttpCompletionOption.ResponseContentRead, cancellation)
                .ReceiveJson<SymbolPriceResponse>();
        Result<SymbolPriceResponse> result = await symbolPriceFn.CallAsync(flurlClient, null, cancellationToken);
        if (result.Error != null)
        {
            return -1;
        }
        Console.WriteLine($"Price: {result.Data!.Price}");
        return decimal.Parse(result.Data!.Price, CultureInfo.InvariantCulture);
    }



    public async Task<string> GetKlinesAsync(string symbol, string startTime, string? endTime, CancellationToken cancellationToken)
    {
        using var flurlClient = FlurlExtensions.GetFlurlClient(_binanceOptions);
        var klinesFn = async (IFlurlClient client, object? requestData, CancellationToken cancellation) => 
            await client.Request()
                .AppendPathSegments("api", "v3", "klines")
                .SetQueryParam("symbol", symbol)
                .SetQueryParam("interval", "1m")
                // .SetQueryParam("limit", 1)
                .SetQueryParam("startTime", startTime.ToUtcEpoch())
                .SetQueryParam("endTime", endTime?.ToUtcEpoch())
                .GetAsync(HttpCompletionOption.ResponseContentRead, cancellation)
                .ReceiveString();
        Result<string> result = await klinesFn.CallAsync(flurlClient, null, cancellationToken);
        if (result.Error != null)
        {
            return string.Empty;
        }
        var klines = result.Data!.ToKlineArray();
        var length  = klines.Data!.Length;
        return result.Data!;
    }
}