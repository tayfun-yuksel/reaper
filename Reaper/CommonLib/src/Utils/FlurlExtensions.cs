using System.Text.Json;
using Flurl.Http;

namespace Reaper.CommonLib.Utils;
public static class FlurlExtensions
{
    
    public static IFlurlClient GetFlurlClient(string baseUrl, bool enableLogging)
    {
        var client = new FlurlClient(baseUrl);
        client.BeforeCall(async call =>
        {
            var requestHeaders = call.Request.Headers;
            var requestContent = call.Request.Content == null ? null : await call.Request.Content.ReadAsStreamAsync();
            if (!enableLogging) return;
            Console.WriteLine("OnBeforeCall:");
            Console.WriteLine($"Request-Url: {call.Request.Url}");
            Console.WriteLine($"Request-Headers: {JsonSerializer.Serialize(requestHeaders)}");
            Console.WriteLine($"Request-Content: {requestContent}");
        });

        client.OnError(async call =>
        {
            if (!enableLogging) return;
            Console.WriteLine("OnError:");
            Console.WriteLine($"Request-Url: {call.Request.Url}");
            Console.WriteLine($"Error: {call.Exception.Message}");
            Console.WriteLine($"Response: {await call.Response.GetStringAsync()}");
        });

        client.AfterCall(async call =>
        {
            if (!enableLogging) return;
            Console.WriteLine("OnAfterCall:");
            Console.WriteLine($"Request-Url: {call.Request.Url}");
            Console.WriteLine($"Response: {await call.Response.GetStringAsync()}");
        });
        return client;
    }
}