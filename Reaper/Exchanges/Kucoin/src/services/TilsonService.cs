using Reaper.CommonLib.Interfaces;
using Reaper.Exchanges.Kucoin.Interfaces;
using Reaper.SignalSentinel.Strategies;

namespace Reaper.Exchanges.Kucoin.Services;
public class TilsonService(IMarketDataService marketDataService,
    IBrokerService brokerService,
    IPositionInfoService positionInfoService) : ITilsonService
{
    internal async Task<SignalType> GetTargetActionAsync(
            SignalType position,
            string symbol,
            int interval,
            CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow.AddMinutes(-(interval * 50)).ToString("dd-MM-yyyy HH:mm");
        var endTime = DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm");
        var pricesResult = await marketDataService.GetKlinesAsync(symbol, startTime, endTime, interval, cancellationToken);

        if (pricesResult.Error != null)
        {
            throw new InvalidOperationException("Error getting klines", pricesResult.Error);
        }

        var t3Values = TilsonT3.CalculateT3([.. pricesResult.Data], period: 6, volumeFactor: 0.5m);

        var t3Last = t3Values.Last();
        var originLast = pricesResult.Data!.Last();

        if (position != SignalType.Buy && t3Last > originLast)
        {
            RLogger.AppLog.Information($"Buy signal detected for {symbol} at {DateTime.UtcNow}");
            return SignalType.Buy;
        }
        else if (position != SignalType.Sell && t3Last < originLast)
        {
            RLogger.AppLog.Information($"Sell signal detected for {symbol} at {DateTime.UtcNow}");
            return SignalType.Sell;
        }

        RLogger.AppLog.Information($"Holding {symbol} at {DateTime.UtcNow}");
        return SignalType.Hold;
    }




    public async Task TryBuyOrSellAsync(string symbol, SignalType actionToTake, decimal amount, CancellationToken cancellationToken)
    {
        if (actionToTake == SignalType.Buy)
        {
            RLogger.AppLog.Information($"BUYING: ..............{symbol} at {DateTime.UtcNow}");
            RLogger.AppLog.Information($"AMOUNT: ..............{amount}\n");
            await brokerService.BuyMarketAsync(symbol, amount, cancellationToken);
        }
        else if (actionToTake == SignalType.Sell)
        {
            RLogger.AppLog.Information($"SELLING..............{symbol} at {DateTime.UtcNow}");
            RLogger.AppLog.Information($"AMOUNT: .............{amount}\n");
            await brokerService.SellMarketAsync(symbol, amount, cancellationToken);
        }
    }


    internal static SignalType GetOppositeAction(SignalType side)
    {
        if (side == SignalType.Buy)
        {
            return SignalType.Sell;
        }
        else if (side == SignalType.Sell)
        {
            return SignalType.Buy;
        }
        return side;
    }


    internal async Task<Result<(bool takeProfit, decimal percent)>> WatchTargetProfitAsync(
        string symbol,
        decimal entryPrice,
        decimal targetPnlPercent,
        CancellationToken cancellationToken)
    {
        var markPrice = await marketDataService.GetSymbolPriceAsync(symbol, cancellationToken);

        if (markPrice.Error != null)
        {
            return new() { Error = markPrice.Error };
        }
        var currentProfitRatio = (markPrice.Data - entryPrice) / entryPrice;

        if (currentProfitRatio >= targetPnlPercent)
        {
            return new() { Data = (true, currentProfitRatio) };
        }
        return new() { Data = (false, currentProfitRatio) };
    }


    public async Task RunAsync(
        string symbol,
        decimal amount,
        decimal profitPercentage,
        int interval,
        CancellationToken cancellationToken)
    {
        TimeSpan profitTimeOut = TimeSpan.FromMinutes(interval);
        SignalType currentAction = SignalType.Undefined;
        var positionDetail = async(SignalType currentPosition) => 
        {
            if (currentPosition == SignalType.Undefined)
            {
                return (tradeAmount: amount, enterPrice: 0);
            }

            var positionDetails = await positionInfoService.GetPositionInfoAsync(symbol, cancellationToken);
            var (tradeAmount, enterPrice, _) = positionDetails.Data!; 
            return (tradeAmount, enterPrice);
        };

        while (cancellationToken.IsCancellationRequested == false)
        {
            SignalType actionToTake = await GetTargetActionAsync(currentAction,
                symbol,
                interval,
                cancellationToken);

            //close position before target action, if in position
            if ((currentAction == SignalType.Buy || currentAction == SignalType.Sell)
                && actionToTake != SignalType.Hold )
            {
                RLogger.AppLog.Information($"CLOSING {currentAction} POSITION....");
                RLogger.AppLog.Information($"BEFORE: {actionToTake}....");

                await TryBuyOrSellAsync(
                    symbol,
                    actionToTake,
                    (await positionDetail(currentAction)).tradeAmount,
                    cancellationToken);
                
            }
            //open position
            await TryBuyOrSellAsync(symbol,
                actionToTake, 
                (await positionDetail(currentAction)).tradeAmount,
                cancellationToken);

            if (actionToTake == SignalType.Buy || actionToTake == SignalType.Sell)
            {
                currentAction = actionToTake;
            }

            //try to take profit
            using var timeOutCTS = new CancellationTokenSource(profitTimeOut);
            timeOutCTS.Token.Register(() =>
            {
                RLogger.AppLog.Information("PROFIT TIMEOUT REACHED.........");
                RLogger.AppLog.Information("STOPPING PROFIT WATCH....");
            });

            while (timeOutCTS.IsCancellationRequested == false)
            {
                await Task.Delay(5 * 1000, cancellationToken);
                var profit = await WatchTargetProfitAsync(
                                    symbol,
                                    (await positionDetail(currentAction)).enterPrice,
                                    profitPercentage,
                                    cancellationToken);

                if (profit.Error != null)
                {
                    RLogger.AppLog.Information($"ERROR WATHING PROFIT.....: {profit.Error}");
                    continue;
                }

                var (takeProfit, profitPercent) = profit.Data!;
		RLogger.AppLog.Information($"takeProfit: {takeProfit},  profitPercent: {profitPercent}");
                if (takeProfit)
                {
                    var profitAmount = (await positionDetail(currentAction)).tradeAmount * profitPercent;
                    RLogger.AppLog.Information($"REALISED PNL: {profitPercent}");
                    RLogger.AppLog.Information("TAKING PROFIT....");
                    RLogger.AppLog.Information($"PROFIT AMOUNT: {profitAmount}");

                    await TryBuyOrSellAsync(symbol, GetOppositeAction(currentAction), profitAmount, cancellationToken);
                }
            }
        }
    }
}
