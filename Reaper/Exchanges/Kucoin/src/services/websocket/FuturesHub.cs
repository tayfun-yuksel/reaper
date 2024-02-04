using System.Dynamic;
using System.Net.WebSockets;
using Flurl.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Reaper.CommonLib.Interfaces;
using Reaper.Exchanges.Kucoin.Interfaces;
using Reaper.Exchanges.Kucoin.Services.Models;

namespace Reaper.Exchanges.Kucoin.Services;
public class FuturesHub(IOptions<KucoinOptions> options,
    IPositionInfoService positionInfoService) : IFuturesHub
{
    private readonly KucoinOptions _kucoinOptions = options.Value;

    private async Task<(string endpoint, string token)> GetDynamicUrlAsync()
    {
        using var flurlClient = CommonLib.Utils.FlurlExtensions
            .GetFlurlClient(RLogger.HttpLog, _kucoinOptions.FuturesBaseUrl, true);

        var getPublicEndpointsFn = async () => await flurlClient.Request()
                .AppendPathSegments("api", "v1", "bullet-private")
                .WithSignatureHeaders(_kucoinOptions, "POST")
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
    



    internal Func<string, CancellationToken, Task<Result<ClientWebSocket>>> MarketDataWebSocket => async(string symbol, CancellationToken cancellationToken) =>
    {
        ClientWebSocket webSocket = new();

        var subscribeFn = async() =>
        {
            var (endpoint, token) = await GetDynamicUrlAsync();
            Uri futuresUrl = new($"{endpoint}?token={token}");
            await webSocket.ConnectAsync(futuresUrl, cancellationToken);

            string messageJson = JsonConvert.SerializeObject(new
            {
                privateChannel = true,
                type = "subscribe",
                topic = $"/contract/instrument:{symbol.ToUpper()}", 
            });
            ArraySegment<byte> bytesToSend = new(System.Text.Encoding.UTF8.GetBytes(messageJson));
            await webSocket.SendAsync(bytesToSend, WebSocketMessageType.Text, true, cancellationToken);
        };

        Exception? exception = await subscribeFn
            .WithErrorPolicy(RetryPolicies.WebSocketLogAndRetryPolicy)
            .CallAsync();

        if (exception != null)
        {
            return new() { Error = exception };
        }
        return new() { Data = webSocket };

    };




    // public async Task<Result<(bool takeProfit, decimal profitPercent)>> WatchTargetProfitAsync(
    //     string symbol,
    //     decimal entryPrice,
    //     decimal targetPnlPercent,
    //     TimeSpan watchTime)
    // {
    //     using var timeOutCTS = new CancellationTokenSource(watchTime);
    //     var webSocketResult = await MarketDataWebSocket(symbol, timeOutCTS.Token);

    //     if (webSocketResult.Error != null)
    //     {
    //         return new() { Error = webSocketResult.Error };
    //     }

    //     using var marketDataConnection = webSocketResult.Data!;

    //     timeOutCTS.Token.Register(() =>
    //     {
    //         Console.WriteLine("WatchProfit timeout. Closing websocket.");
    //         marketDataConnection.Abort();
    //     });


    //     while (marketDataConnection.State == WebSocketState.Open && !timeOutCTS.IsCancellationRequested)
    //     {
    //         var buffer = new byte[1024 * 4];
    //         var receiveFn = async() => await marketDataConnection.ReceiveAsync(buffer, timeOutCTS.Token);
    //         var socketMsgResult = await receiveFn
    //             .WithErrorPolicy(RetryPolicies.WebSocketLogAndRetryPolicy)
    //             .CallAsync();
            
    //         if (socketMsgResult.Error != null)
    //         {
    //             return new() { Error = socketMsgResult.Error };
    //         }

    //         if (socketMsgResult.Data!.MessageType == WebSocketMessageType.Text)
    //         {
    //             string response = System.Text.Encoding.UTF8.GetString(buffer, 0, socketMsgResult.Data!.Count);
    //             RLogger.AppLog.Information(
    //                 $"WatchProfit received message: ",
    //                 response);

    //             dynamic marketData = JsonConvert.DeserializeObject<ExpandoObject>(response);

    //             if (marketData.type != "message" || marketData.subject != "mark.index.price")
    //             {
    //                 RLogger.AppLog.Information(
    //                     $"Received message is not a position change",
    //                     response,
    //                     ConsoleColor.Red);
    //                 continue;
    //             }

    //             var markPrice = (decimal)marketData.data.markPrice;
    //             var currentProfitPercent = markPrice / entryPrice; 

    //             if (currentProfitPercent >= targetPnlPercent)
    //             {
    //                 return new() { Data = (true, currentProfitPercent) };
    //             }
    //         }
    //     } 
    //     return new() { Data = (false, 0) };
    // }
}