using System.Dynamic;
using Flurl.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Reaper.CommonLib.Interfaces;
using Reaper.Exchanges.Kucoin.Services.Converters;
using Reaper.Exchanges.Kucoin.Services.Models;

namespace Reaper.Exchanges.Kucoin.Services;
public class FuturesMarketDataService(IOptions<KucoinOptions> options) : IMarketDataService
{
    private readonly KucoinOptions _kucoinOptions = options.Value;


    public async Task<decimal> GetSymbolPriceAsync(string symbol, CancellationToken cancellationToken)
    {
        using var flurlClient = CommonLib.Utils.FlurlExtensions.GetFlurlClient(_kucoinOptions.FuturesBaseUrl, true);

        var getSymbolPriceFn = async (IFlurlClient client, object? requestData, CancellationToken cancellationToken) =>
            await client.Request()
                .AppendPathSegments("api", "v1", "contracts", symbol)
                .WithSignatureHeaders(_kucoinOptions, "GET") 
                .GetAsync(HttpCompletionOption.ResponseContentRead, cancellationToken)
                .ReceiveString();

        Result<string> result = await getSymbolPriceFn.CallAsync(flurlClient, new
        {
            symbol = symbol.ToUpper()
        }, cancellationToken);

        if (result.Error != null)
        {
            return 0;
        }

        dynamic response = JsonConvert.DeserializeObject<ExpandoObject>(result.Data!);
        decimal markPrice = (decimal)response.data.indexPrice;
        return markPrice;
    }



    public async Task<IEnumerable<decimal>> GetKlinesAsync(string symbol,
        string startTime,
        string? endTime,
        int interval,
        CancellationToken cancellationToken)
    {
        using var flurlClient = CommonLib.Utils.FlurlExtensions.GetFlurlClient(_kucoinOptions.FuturesBaseUrl, true);

        var klinesFn = async (IFlurlClient client, object? requestData, CancellationToken cancellation) =>
            await client.Request()
                .AppendPathSegments("api", "v1", "kline", "query")
                .SetQueryParams(requestData)
                .GetAsync(HttpCompletionOption.ResponseContentRead, cancellation)
                .ReceiveString();

        Result<string> result = await klinesFn.CallAsync(flurlClient, new
        {
            symbol = symbol.ToUpper(),
            granularity = interval,
            from = startTime.ToUtcEpochMs(),
            to = endTime?.ToUtcEpochMs()
        }, cancellationToken);

        if (result.Error != null)
        {
            Console.WriteLine($"Error: {result.Error.Message}");
            return [];
        }

        var klines = result.Data!.ToFuturesKlineArray().Data;
        var prices = klines!
            .Select(x => x.ClosePrice)
            .ToList() as IEnumerable<decimal>;

        return prices!;
    }
}