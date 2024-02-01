using System.Dynamic;
using System.Net.WebSockets;
using Flurl.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using Reaper.CommonLib.Interfaces;
using Reaper.Exchanges.Kucoin.Interfaces;
using Reaper.Exchanges.Kucoin.Services.Models;
using Reaper.SignalSentinel.Strategies;

namespace Reaper.Exchanges.Kucoin.Services;
public class FuturesHub(IOptions<KucoinOptions> options) : IFuturesHub
{
    private readonly KucoinOptions _kucoinOptions = options.Value;
    private readonly ClientWebSocket webSocket = new();

    private static AsyncRetryPolicy WebSocketRetryPolicy => Policy
        .Handle<WebSocketException>()
        .WaitAndRetryAsync(
            3,
            retryAttempt => TimeSpan.FromSeconds(2),
            (exception, timeSpan, retryCount, context) => 
            {
                Console.WriteLine($"Retrying after {timeSpan.Seconds}");
                Console.WriteLine($"seconds due to: {exception.Message}");
                Console.WriteLine($"RetryCount: {retryCount}");
            });



    private async Task<(string endpoint, string token)> GetFuturesDynamicWebSocketUrl()
    {
        using var flurlClient = CommonLib.Utils.FlurlExtensions.GetFlurlClient(_kucoinOptions.FuturesBaseUrl, true);

        var getPublicEndpointsFn = async (IFlurlClient client, object? requestData, CancellationToken cancellationToken) =>
            await client.Request()
                .AppendPathSegments("api", "v1", "bullet-private")
                .WithSignatureHeaders(_kucoinOptions, "POST")
                .PostAsync()
                .ReceiveString();

        Result<string> result = await getPublicEndpointsFn.CallAsync(flurlClient, null, CancellationToken.None);

        if (result.Error != null)
        {
            throw new Exception(result.Error.Message);
        }

        dynamic response = JsonConvert.DeserializeObject<ExpandoObject>(result.Data!);
        string endpoint = response.data.instanceServers[0].endpoint;
        string token = response.data.token;
        return (endpoint, token);
    }
    



    private async Task SubscribeToMarketDataAsync(string symbol)
    {
        var (endpoint, token) = await GetFuturesDynamicWebSocketUrl();
        Uri futuresUrl = new($"{endpoint}?token={token}");
        await webSocket.ConnectAsync(futuresUrl, CancellationToken.None);

        string messageJson = JsonConvert.SerializeObject(new
        {
            privateChannel = true,
            type = "subscribe",
            topic = $"/contract/position:{symbol.ToUpper()}", 
        });
        ArraySegment<byte> bytesToSend = new(System.Text.Encoding.UTF8.GetBytes(messageJson));
        await webSocket.SendAsync(bytesToSend, WebSocketMessageType.Text, true, CancellationToken.None);
    }




    public async Task<(bool takeProfit, decimal profit)> WatchTargetProfitAsync(decimal targetPnl, TimeSpan watchTime, string symbol)
    {
        var watchForProfitFn = async () => 
        {
            await SubscribeToMarketDataAsync(symbol);
            using var cts = new CancellationTokenSource(watchTime);

            while (webSocket.State == WebSocketState.Open && !cts.IsCancellationRequested)
            {
                var buffer = new byte[1024 * 4];
                WebSocketReceiveResult result = 
                    await webSocket.ReceiveAsync(buffer, cts.Token);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string response = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Utils.Print(
                        $"WatchProfit received message: ",
                        response,
                        ConsoleColor.Green);
                    dynamic marketData = JsonConvert.DeserializeObject<ExpandoObject>(response);

                    if (marketData.type != "message" || marketData.subject != "position.change")
                    {
                        Utils.Print(
                            $"Received message is not a position change",
                            response,
                            ConsoleColor.Red);
                        continue;
                    }

                    var realisedPnl = (decimal)marketData.data.realisedPnl;
                    if (realisedPnl >= targetPnl)
                    {
                        return (true, realisedPnl);
                    }
                }
            } 
            return (false, 0m);
        };


        var response = await watchForProfitFn
            .WithPolicy(WebSocketRetryPolicy)
            .WithErrorLogging();

        if (response.Error != null)
        {
            if (response.Error is OperationCanceledException)
            {
                Console.WriteLine("Watch time ended. Operation cancelled......");
            }
            return (false, 0m);
        }

        return response.Data;
    }


}