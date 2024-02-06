using System.Dynamic;
using System.Net.WebSockets;
using Flurl.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Reaper.CommonLib.Interfaces;
using Reaper.Exchanges.Kucoin.Interfaces;
using Reaper.Exchanges.Kucoin.Services.Models;
using Reaper.SignalSentinel.Strategies;

namespace Reaper.Exchanges.Kucoin.Services;
public class FuturesHub(IOptions<KucoinOptions> options,
    IPositionInfoService positionInfoService) : IFuturesHub
{
    private readonly KucoinOptions _kucoinOptions = options.Value;

    private async Task<(string endpoint, string token)> GetDynamicUrlAsync()
    {
        using var flurlClient = FlurlExtensions.GetHttpClient(_kucoinOptions);

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
    



    internal async Task<Result<ClientWebSocket>> MarketDataWebSocket(string symbol, CancellationToken cancellationToken)
    {
        ClientWebSocket webSocket = new();

        var subscribeFn = async() =>
        {
            var (endpoint, token) = await GetDynamicUrlAsync();
            Uri futuresUrl = new($"{endpoint}?token={token}");
            await webSocket.ConnectAsync(futuresUrl, cancellationToken);

            string messageJson = JsonConvert.SerializeObject(new
            {
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

    }




    public async Task<Result<(bool takeProfit, decimal profitPercent)>> WatchTargetProfitAsync(
        SignalType currentPosition,
        string symbol,
        decimal entryPrice,
        decimal targetPnlPercent,
        CancellationToken cancellationToken)
    {
        var marketSocket = await MarketDataWebSocket(symbol, cancellationToken);

        if (marketSocket.Error != null)
        {
            return new() { Error = marketSocket.Error };
        }

        using var marketDataConnection = marketSocket.Data!;

        while (marketDataConnection.State == WebSocketState.Open 
                && !cancellationToken.IsCancellationRequested)
        {
            var buffer = new byte[1024 * 4];
            var receiveFn = async() => await marketDataConnection.ReceiveAsync(buffer, cancellationToken);

            var socketMsgResult = await receiveFn
                .WithErrorPolicy(RetryPolicies.WebSocketLogAndRetryPolicy)
                .CallAsync();
            
            if (socketMsgResult.Error != null)
            {
                return new() { Error = socketMsgResult.Error };
            }

            if (socketMsgResult.Data!.MessageType == WebSocketMessageType.Text)
            {
                string response = System.Text.Encoding.UTF8.GetString(buffer, 0, socketMsgResult.Data!.Count);

                dynamic marketData = JsonConvert.DeserializeObject<ExpandoObject>(response);

                if (marketData.type != "message" || marketData.subject != "mark.index.price")
                {
                    RLogger.AppLog.Information($"subject is not a mark.index.price ");
                    continue;
                }


                var markPrice = (decimal)marketData.data.markPrice;
                var currentProfitPercent = currentPosition == SignalType.Buy 
                    ? (markPrice - entryPrice) / entryPrice
                    : (entryPrice - markPrice) / entryPrice;


                RLogger.AppLog.Information($"SYMBOL: {symbol}");
                RLogger.AppLog.Information($"ENTRY-PRICE: {entryPrice}");
                RLogger.AppLog.Information($"MARKET-PRICE: {markPrice}");
                RLogger.AppLog.Information($"CURRENT-PROFIT: {currentProfitPercent}\n\n");

                if (currentProfitPercent >= targetPnlPercent)
                {
                    RLogger.AppLog.Information($"PROFIT TARGET REACHED.........WS"); 
                    return new() { Data = (true, currentProfitPercent) };
                }
            }
        } 
        return new() { Data = (false, 0) };
    }



    internal async Task<Result<ClientWebSocket>> PositionChangeWebsocket(string symbol, CancellationToken cancellationToken)
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
                topic = $"/contract/position:{symbol.ToUpper()}", 
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

    }
    public async Task<Result<(bool takeProfit, decimal profitPnl)>> MonitorPositionChangeAsync(
        string symbol,
        decimal targetPnl,
        CancellationToken cancellationToken)
    {
        var positionSocket = await PositionChangeWebsocket(symbol, cancellationToken);
        using var positionChange = positionSocket.Data!;

        while (cancellationToken.IsCancellationRequested == false)
        {
            var buffer = new byte[1024 * 4];
            var receiveFn = async() => await positionChange.ReceiveAsync(buffer, cancellationToken);

            var socketMsgResult = await receiveFn
                .WithErrorPolicy(RetryPolicies.WebSocketLogAndRetryPolicy)
                .CallAsync();
            
            if (socketMsgResult.Error != null)
            {
                return new() { Error = socketMsgResult.Error };
            }

            try
            {
                var responseStr = System.Text.Encoding.UTF8.GetString(buffer, 0, socketMsgResult.Data!.Count);
                dynamic positionData = JsonConvert.DeserializeObject<ExpandoObject>(responseStr);

                if (positionData.type != "message" || positionData.subject != "position.change")
                {
                    RLogger.AppLog.Information($"Received message is not a position change");
                    continue;
                }
                RLogger.AppLog.Information($"Response: {responseStr}");

                var pnl = (decimal)positionData.data.realisedPnl;
                if (pnl >= targetPnl)
                {
                    RLogger.AppLog.Information($"PROFIT TARGET REACHED.........WS {symbol}: {pnl}");
                    return new() { Data = (true, pnl) };
                }
            }
            catch (Exception ex)
            {
                RLogger.AppLog.Information($"Error parsing position change message: {ex.Message}");
                return new() { Error = ex };
            }
        }
        return new() { Data = (false, 0) };
    }
}