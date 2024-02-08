using System.Dynamic;
using System.Net.WebSockets;
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


    public async Task<Result<(bool takeProfit, decimal profitPercent)>> WatchTargetProfitAsync(
        SignalType currentPosition,
        string symbol,
        decimal entryPrice,
        decimal targetPnlPercent,
        CancellationToken cancellationToken)
    {
        string topic = JsonConvert.SerializeObject(new
        {
            type = "subscribe",
            topic = $"/contract/instrument:{symbol.ToUpper()}", 
        });

        var marketSocket = await WsExtensions.GetWebSocket(
            _kucoinOptions,
            topic,
            cancellationToken);

        if (marketSocket.Error != null)
        {
            return new() { Error = marketSocket.Error };
        }

        using var marketDataConnection = marketSocket.Data!;

        while (marketDataConnection.State == WebSocketState.Open 
                && !cancellationToken.IsCancellationRequested)
        {
            var response = await WsExtensions.ReadAsync(
                marketDataConnection,
                cancellationToken);

            if (response.Error != null)
            {
                return new() { Error = response.Error };
            }

            var responseStr = response.Data!;
            dynamic marketData = JsonConvert.DeserializeObject<ExpandoObject>(responseStr);

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
            RLogger.AppLog.Information($"POSITION: {currentPosition}");
            RLogger.AppLog.Information($"ENTRY-PRICE: {entryPrice}");
            RLogger.AppLog.Information($"MARKET-PRICE: {markPrice}");
            RLogger.AppLog.Information($"CURRENT-PROFIT: {currentProfitPercent}\n\n");

            if (currentProfitPercent >= targetPnlPercent)
            {
                RLogger.AppLog.Information($"PROFIT TARGET REACHED.........WS"); 
                return new() { Data = (true, currentProfitPercent) };
            }
        } 

        return new() { Data = (false, 0) };
    }





    public async Task<Result<(bool takeProfit, decimal profitPnl)>> MonitorPositionChangeAsync(
        string symbol,
        decimal targetPnl,
        CancellationToken cancellationToken)
    {
        string messageJson = JsonConvert.SerializeObject(new
        {
            privateChannel = true,
            type = "subscribe",
            topic = $"/contract/position:{symbol.ToUpper()}", 
        });

        var positionSocket = await WsExtensions.GetWebSocket(
            _kucoinOptions,
            messageJson,
            cancellationToken);

        using var positionChange = positionSocket.Data!;

        while (cancellationToken.IsCancellationRequested == false)
        {
            try
            {
                var response = await WsExtensions.ReadAsync(positionChange, cancellationToken);
                if (response.Error != null)
                {
                    return new() { Error = response.Error };
                }
                var responseStr = response.Data!;

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