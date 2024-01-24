using System.Globalization;
using System.Text.Json;
using Flurl.Http;
using Microsoft.Extensions.Options;
using Reaper.CommonLib.Utils;
using Reaper.Exchanges.Kucoin.Services.Converters;
using Reaper.Exchanges.Kucoin.Services.Models;

namespace Reaper.Exchanges.Kucoin.Services;
public class MarketDataService(IOptions<KucoinOptions> options) : IMarketDataService
{
    private readonly KucoinOptions _kucoinOptions = options.Value;

    public async Task<IEnumerable<decimal>> GetFutureKlinesAsync(string symbol,
        string startTime,
        string? endTime,
        string interval,
        CancellationToken cancellationToken)
    {
        // GET /api/v1/market/candles?type=1min&symbol=BTC-USDT&startAt=1566703297&endAt=1566789757
        using var flurlClient = CommonLib.Utils.FlurlExtensions.GetFlurlClient(_kucoinOptions.BaseUrl, true);
        var klinesFn = async (IFlurlClient client, object? requestData, CancellationToken cancellation) =>
            await client.Request()
                .AppendPathSegments("api", "v1", "kline", "query")
                .SetQueryParams(new
                {
                    symbol = symbol,
                    granularity = interval,
                    from = startTime.ToUtcEpoch(),
                    to = endTime?.ToUtcEpoch()
                })
                .GetAsync(HttpCompletionOption.ResponseContentRead, cancellation)
                .ReceiveString();
        Result<string> result = await klinesFn.CallAsync(flurlClient, null, cancellationToken);
        var klines = result.Data!.ToFuturesKlineArray().Data;
        int count = 0;
        klines!.ForEach(x =>
        {
            Console.WriteLine($"Kline {count++}: {x.Time.FromUtcMillliSeconds()}"); // 0 is the timestamp
            Console.WriteLine($"price: {x.ClosePrice}");
        });
        var prices = klines
            .Select(x => x.ClosePrice)
            .ToList() as IEnumerable<decimal>;
        
        return prices!;
    }
}