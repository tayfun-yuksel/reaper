using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Flurl;
using Flurl.Http;
using Reaper.Exchanges.Binance.Services.Configuration;

namespace Reaper.Exchanges.Binance.Services;
public static class FlurlExtensions
{
    private static string CalculateSignature(string secretKey, string data)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    public static IFlurlRequest WithSignedQueryParams(this IFlurlRequest request, string secretKey, object? data)
    {
        var dataDict = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(data));
        var queryParams = dataDict?
            .OrderBy(x => x.Key)
            .Append(new KeyValuePair<string, object>("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()))
            ?? new Dictionary<string, object>(){ { "timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }};
        request.SetQueryParams(queryParams);
        string queryString = string.Join("&", queryParams?.Select(x => $"{x.Key}={x.Value}") ?? []);
        var signature = CalculateSignature(secretKey, queryString);
        request.SetQueryParam("signature", signature);
        Console.WriteLine($"QueryParam: {request.Url}");
        return request;
    }


    // public static IFlurlClient GetFlurlClient(BinanceOptions binanceOptions)
    // {
    //     var client = new FlurlClient(binanceOptions.BaseURL
    //         ?? throw new InvalidOperationException("Binance:BaseURL is not configured"));
    //     client.BeforeCall(async call =>
    //     {
    //         Console.WriteLine("OnBeforeCall:");
    //         Console.WriteLine($"Request-Url: {call.Request.Url}");
    //         var requestHeaders = call.Request.Headers;
    //         var requestContent = call.Request.Content == null ? null : await call.Request.Content.ReadAsStreamAsync();
    //         Console.WriteLine($"Request-Headers: {JsonSerializer.Serialize(requestHeaders)}");
    //         Console.WriteLine($"Request-Content: {requestContent}");
    //     });

    //     client.OnError(async call =>
    //     {
    //         Console.WriteLine("OnError:");
    //         Console.WriteLine($"Request-Url: {call.Request.Url}");
    //         Console.WriteLine($"Error: {call.Exception.Message}");
    //         Console.WriteLine($"Response: {await call.Response.GetStringAsync()}");
    //     });

    //     client.AfterCall(async call =>
    //     {
    //         Console.WriteLine("OnAfterCall:");
    //         Console.WriteLine($"Request-Url: {call.Request.Url}");
    //         // Console.WriteLine($"Response: {await call.Response.GetStringAsync()}");
    //     });
    //     return client;
    // }

    public static async Task<Result<TResponse>> CallAsync<TResponse>(
        this Func<IFlurlClient, object?, CancellationToken, Task<TResponse>> flurlCall,
        IFlurlClient client,
        object? data,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await flurlCall(client, data, cancellationToken);
            return new() { Data = response };
        }
        catch (FlurlHttpException ex)
        {
            return new() { Error = ex };
        }
        catch (Exception ex)
        {
            return new() { Error = ex };
        }
        
    }



    
}