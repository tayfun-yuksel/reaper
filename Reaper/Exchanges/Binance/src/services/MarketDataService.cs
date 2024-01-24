
using System.Globalization;
using Flurl.Http;
using Microsoft.Extensions.Options;
using Reaper.Exchanges.Binance.Services.ApiModels;
using Reaper.Exchanges.Binance.Services.Configuration;
using Reaper.Exchanges.Binance.Services.Converters;

namespace Reaper.Exchanges.Binance.Services;
public class MarketDataService(IOptions<BinanceOptions> options) : IMarketDataService
{
    private readonly BinanceOptions _binanceOptions = options.Value;

    public async Task<SymbolExchangeInfoResponse> GetSymbolExchangeInfoAsync(string symbol, CancellationToken cancellationToken)
    {
        using var flurlClient = CommonLib.Utils.FlurlExtensions.GetFlurlClient(_binanceOptions.BaseURL, false);
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
        using var flurlClient = CommonLib.Utils.FlurlExtensions.GetFlurlClient(_binanceOptions.BaseURL, false);
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



    public async Task<IEnumerable<decimal>> GetKlinesAsync(string symbol,
        string startTime,
        string? endTime,
        string interval,
        CancellationToken cancellationToken)
    {
        using var flurlClient = CommonLib.Utils.FlurlExtensions.GetFlurlClient(_binanceOptions.BaseURL, false);
        var klinesFn = async (IFlurlClient client, object? requestData, CancellationToken cancellation) => 
            await client.Request()
                .AppendPathSegments("api", "v3", "klines")
                .SetQueryParam("symbol", symbol)
                .SetQueryParam("interval", interval)
                .SetQueryParam("limit", 50)
                // .SetQueryParam("contractType", "PERPETUAL")
                // .SetQueryParam("startTime", startTime.ToUtcEpoch())
                .SetQueryParam("endTime", endTime?.ToUtcEpoch())
                .GetAsync(HttpCompletionOption.ResponseContentRead, cancellation)
                .ReceiveString();

        Result<string> result = await klinesFn.CallAsync(flurlClient, null, cancellationToken);
        if (result.Error != null)
        {
            return [];
        }
        var klines = result.Data!.ToKlineArray().Data!;
        int count = 0;
        for (int i = klines.Length; i > 0; i--)
        {
            var x = klines[i - 1];
            Console.WriteLine($"Kline: {count++}: {x.CloseTime.FromUtcEpoch()}");
            Console.WriteLine($"price: {x.Close}:");
        }
        var prices = klines
            .Select(x => decimal.Parse(x.Close, CultureInfo.InvariantCulture))
            .ToList();

        return prices;
    }
}