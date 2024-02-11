using System.Text.Json;
using Flurl.Http;
using Microsoft.Extensions.Options;
using Reaper.CommonLib.Interfaces;
using Reaper.Exchanges.Kucoin.Services.Models;

namespace Reaper.Exchanges.Kucoin.Services;
public class FuturesMarketDataService(IOptions<KucoinOptions> options) : IMarketDataService
{
    private readonly KucoinOptions _kucoinOptions = options.Value;
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<Result<IEnumerable<SymbolDetail>>> GetSymbolsAsync(CancellationToken cancellationToken)
    {
        using var flurlClient = FlurlExtensions.GetHttpClient(_kucoinOptions);

        var getSymbolsFn = async () => await flurlClient.Request()
                .AppendPathSegments("api", "v1", "contracts", "active")
                .WithSignatureHeaders(_kucoinOptions, "GET")
                .GetStreamAsync(HttpCompletionOption.ResponseContentRead, cancellationToken);

        Result<Stream> result = await getSymbolsFn
            .WithErrorPolicy(RetryPolicies.HttpErrorLogAndRetryPolicy)
            .CallAsync();

        if (result.Error != null)
        {
            return new() { Error = result.Error };
        }


        var symbols = await JsonSerializer.DeserializeAsync<IEnumerable<SymbolDetail>>(
            result.Data!,
            JsonOptions,
            cancellationToken: cancellationToken);

        return new() { Data = symbols };
    }


    public async Task<Result<decimal>> GetSymbolPriceAsync(string symbol, CancellationToken cancellationToken)
    {
        using var flurlClient = FlurlExtensions.GetHttpClient(_kucoinOptions);

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

        var response = JsonSerializer.Deserialize<SymbolDetail>(result.Data!, JsonOptions); 
        decimal markPrice = response!.Data.MarkPrice;

        return new(){ Data = markPrice };
    }



    public async Task<Result<IEnumerable<FuturesKline>>> GetKlinesAsync(string symbol,
        string startTime,
        string? endTime,
        int interval,
        CancellationToken cancellationToken)
    {
        using var flurlClient = FlurlExtensions.GetHttpClient(_kucoinOptions);

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
                .GetStreamAsync(HttpCompletionOption.ResponseContentRead, cancellationToken);

        Result<Stream> result = await klinesFn
            .WithErrorPolicy(RetryPolicies.HttpErrorLogAndRetryPolicy)
            .CallAsync();

        if (result.Error != null)
        {
            return new(){ Error = result.Error }; 
        }
        var futuresKlineResponse = await JsonSerializer.DeserializeAsync<FuturesKlineResponse>(
            result.Data!,
            JsonOptions,
            cancellationToken: cancellationToken);


        var klines = futuresKlineResponse!.Data!
            .Select(x => new FuturesKline
            {
                Time = (long)x[0],
                Open = x[1],
                High = x[2],
                Low = x[3],
                Close = x[4],
                Volume = x[5]
            });


        return new(){ Data = klines };
    }
}