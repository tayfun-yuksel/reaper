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


    public async Task<Result<decimal>> GetSymbolPriceAsync(string symbol, CancellationToken cancellationToken)
    {
        using var flurlClient = CommonLib.Utils.FlurlExtensions.GetFlurlClient(RLogger.HttpLog, _kucoinOptions.FuturesBaseUrl, true);

        var getSymbolPriceFn = async () => await flurlClient.Request()
                .AppendPathSegments("api", "v1", "contracts", symbol.ToUpper())
                .WithSignatureHeaders(_kucoinOptions, "GET") 
                .GetAsync(HttpCompletionOption.ResponseContentRead, cancellationToken)
                .ReceiveString();

        Result<string> result = await getSymbolPriceFn
            .WithErrorPolicy(RetryPolicies.HttpErrorLogAndRetryPolicy)
            .CallAsync();


        if (result.Error != null)
        {
            return new(){ Error = result.Error };
        }

        dynamic response = JsonConvert.DeserializeObject<ExpandoObject>(result.Data!);
        decimal markPrice = (decimal)response.data.indexPrice;

        return new(){ Data = markPrice };
    }



    public async Task<Result<IEnumerable<decimal>>> GetKlinesAsync(string symbol,
        string startTime,
        string? endTime,
        int interval,
        CancellationToken cancellationToken)
    {
        using var flurlClient = CommonLib.Utils.FlurlExtensions
            .GetFlurlClient(RLogger.HttpLog, _kucoinOptions.FuturesBaseUrl, true);

        var fromResult = startTime.ToUtcEpochMs();
        var toResult = endTime?.ToUtcEpochMs();
        //todo: handle error
        var queryParams = new
        {
            symbol = symbol.ToUpper(),
            granularity = interval,
            from = fromResult.Data!,
            to = toResult?.Data
        };

        var klinesFn = async () => await flurlClient.Request()
                .AppendPathSegments("api", "v1", "kline", "query")
                .SetQueryParams(queryParams)
                .GetAsync(HttpCompletionOption.ResponseContentRead, cancellationToken)
                .ReceiveString();

        Result<string> result = await klinesFn
            .WithErrorPolicy(RetryPolicies.HttpErrorLogAndRetryPolicy)
            .CallAsync();

        if (result.Error != null)
        {
            return new(){ Error = result.Error }; 
        }
        var klines = result.Data!.ToFuturesKlineArray().Data;
        var prices = klines!
            .Select(x => x.ClosePrice)
            .ToList() as IEnumerable<decimal>;

        return new(){ Data = prices };
    }
}