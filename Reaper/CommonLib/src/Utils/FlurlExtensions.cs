using System.Text.Json;
using Flurl.Http;

namespace Reaper.CommonLib.Utils;
public static class FlurlExtensions
{
    public static IFlurlClient GetFlurlClient(Serilog.ILogger logger, string baseUrl, bool enableLogging)
    {
        var client = new FlurlClient(baseUrl);
        client.BeforeCall(async call =>
        {
            if (!enableLogging) return;

            var requestHeaders = call.Request.Headers;
            var requestContent = call.Request.Content == null ? null : await call.Request.Content.ReadAsStreamAsync();
            logger.Information("OnBeforeCall:");
            logger.Information($"Request-Url: {call.Request.Url}");
            logger.Information($"Request-Headers: {JsonSerializer.Serialize(requestHeaders)}");
            logger.Information($"Request-Content: {requestContent}");
        });

        client.OnError(async call =>
        {
            logger.Error("OnError:");
            logger.Error($"Request-Url: {call.Request.Url}");
            logger.Error($"Error: {call.Exception.Message}");
            logger.Error($"Response: {await call.Response.GetStringAsync()}");
        });

        client.AfterCall(async call =>
        {
            if (!enableLogging) return;
            logger.Information("OnAfterCall:");
            logger.Information($"Request-Url: {call.Request.Url}");
            logger.Information($"Response: {await call.Response.GetStringAsync()}");
        });
        return client;
    }
}