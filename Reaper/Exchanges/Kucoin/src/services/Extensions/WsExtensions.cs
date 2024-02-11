using System.Dynamic;
using System.Net.WebSockets;
using System.Text;
using Flurl.Http;
using Newtonsoft.Json;
using Reaper.CommonLib.Interfaces;
using Reaper.Exchanges.Kucoin.Services.Models;

namespace Reaper.Exchanges.Kucoin.Services;
public static class WsExtensions
{

    private static async Task<(string endpoint, string token)> GetDynamicUrlAsync(KucoinOptions kucoinOptions)
    {
        using var flurlClient = FlurlExtensions.GetHttpClient(kucoinOptions);

        var getPublicEndpointsFn = async () => await flurlClient.Request()
                .AppendPathSegments("api", "v1", "bullet-private")
                .WithSignatureHeaders(kucoinOptions, "POST")
                .PostAsync()
                .ReceiveString();

        Result<string> result = await getPublicEndpointsFn
            .WithErrorPolicy(RetryPolicies.HttpErrorLogAndRetryPolicy)
            .CallAsync();

        if (result.Error != null)
        {
            throw result.Error;
        }

        dynamic response = JsonConvert.DeserializeObject<ExpandoObject>(result.Data!);
        string endpoint = response.data.instanceServers[0].endpoint;
        string token = response.data.token;

        return (endpoint, token);
    }
    



     internal static async Task<Result<ClientWebSocket>> GetWebSocket(
        KucoinOptions kucoinOptions,
        string topic,
        CancellationToken cancellationToken)
    {
        ClientWebSocket webSocket = new();
        var subscribeFn = async() =>
        {
            var (endpoint, token) = await GetDynamicUrlAsync(kucoinOptions);
            Uri futuresUrl = new($"{endpoint}?token={token}");

            await webSocket.ConnectAsync(futuresUrl, CancellationToken.None);

            ArraySegment<byte> bytesToSend = new(Encoding.UTF8.GetBytes(topic));

            await webSocket.SendAsync(
                bytesToSend,
                WebSocketMessageType.Text,
                true,
                cancellationToken);
        };

        Exception? exception = await subscribeFn
            .WithErrorPolicy(RetryPolicies.WebSocketLogAndRetryPolicy)
            .CallAsync();

        if (exception != null)
        {
            return new() { Error = exception };
        }
        return new() { Data = webSocket };
    }



    internal static async Task<Result<string>> ReadAsync(
        ClientWebSocket webSocket,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[1024 * 4];
        var receiveFn = async() => await webSocket.ReceiveAsync(buffer, cancellationToken);

        var response = await receiveFn
            .WithErrorPolicy(RetryPolicies.WebSocketLogAndRetryPolicy)
            .CallAsync();
        
        if (response.Error != null)
        {
            return new() { Error = response.Error };
        }
        return new() { Data = Encoding.UTF8.GetString(buffer, 0, response.Data!.Count) };
    }
}