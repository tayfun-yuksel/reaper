using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Configuration;

namespace Reaper.Exchanges.Binance.Services;
public static class FlurlExtensions
{
    private static string CalculateSignature(string secretKey, string data)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }


    public static IFlurlClient GetFlurlClient(IConfiguration configuration)
    {
        var client = new FlurlClient(configuration["Binance:BaseURL"] 
            ?? throw new InvalidOperationException("Binance:BaseURL is not configured"));
        return client;
    }

    public static async Task<TResponse?> TryReceiveJson<TResponse>(this Task<IFlurlResponse> response)
    {
        ArgumentNullException.ThrowIfNull(response);
        if (response.IsFaulted)
        {
            return default;
        }

        try
        {
            var responseStr = await response.ReceiveString();
            return JsonSerializer.Deserialize<TResponse>(responseStr);
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error while deserializing response: {ex}");
            Console.WriteLine($"Response: {await response.ReceiveString()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while : {ex}");
        }
        return default;
    }
    public static Task<IFlurlResponse> WithLogging(this Task<IFlurlResponse> flurlResponseTask, string logLevel)
    {
        if (flurlResponseTask == null) throw new ArgumentNullException(nameof(flurlResponseTask));

        if (logLevel == "debug" && flurlResponseTask.IsCompletedSuccessfully)
        {
            var response = flurlResponseTask.Result;
            Console.WriteLine($"Response: {response.StatusCode} {response.ResponseMessage}");
        }

        if (flurlResponseTask.IsFaulted)
        {
            var exception = flurlResponseTask.Exception;
            Console.WriteLine($"Exception: {exception}");
        }
        return flurlResponseTask;
    }
    public static async Task<(TResponse?, Exception? error)> CallAsync<TResponse>(
        this Func<IFlurlClient, object, CancellationToken, Task<TResponse>> flurlCall,
        IFlurlClient client,
        object data,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await flurlCall(client, data, cancellationToken);
            return (default, null);
        }
        catch (FlurlHttpException ex)
        {
            return (default, ex);
        }
        catch (Exception ex)
        {
            return (default, ex);
        }
        
    }


    public static IFlurlRequest WithSignature(this IFlurlRequest request, string secretKey, object data = null)
    {
        var queryParams = request.Url.QueryParams.ToDictionary(x => x.Name, x => (string)x.Value);
        if (data != null)
        {
            var jsonParams = JsonSerializer.Deserialize<Dictionary<string, string>>(JsonSerializer.Serialize(data));
            foreach (var (property, value) in jsonParams ?? new Dictionary<string, string>())
            {
                queryParams[property] = value;
            }
        }
        var queryString = queryParams.ToString() ?? string.Empty;
        var signature = CalculateSignature(secretKey, queryString);
        return request.SetQueryParam("signature", signature);
    }
}