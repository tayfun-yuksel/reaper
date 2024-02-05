using Reaper.CommonLib.Interfaces;
using Reaper.Exchanges.Kucoin.Interfaces;
using Reaper.SignalSentinel.Strategies;

namespace Reaper.Exchanges.Kucoin.Services;
public class TilsonService(IMarketDataService marketDataService,
    IBrokerService brokerService,
    IPositionInfoService positionInfoService,
    IFuturesHub futuresHub) : ITilsonService
{
    internal async Task<SignalType> GetTargetActionAsync(
            SignalType position,
            string symbol,
            int interval,
            CancellationToken cancellationToken)
    {
        var endTime = DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm");

        var startTime = DateTime.UtcNow
            .AddMinutes(-(interval * 50))
            .ToString("dd-MM-yyyy HH:mm");

        var pricesResult = await marketDataService.GetKlinesAsync(
            symbol,
            startTime,
            endTime,
            interval,
            cancellationToken);

        if (pricesResult.Error != null)
        {
            throw new InvalidOperationException("Error getting klines", pricesResult.Error);
        }

        var t3Values = TilsonT3.CalculateT3(
            [.. pricesResult.Data],
            period: 6,
            volumeFactor: 0.5m);

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





    internal async Task<(decimal amountInTrade, decimal enterPrice)> GetPositionDetailsAsync(
        string symbol,
        CancellationToken cancellationToken)
    {
        var positionDetails = await positionInfoService.GetPositionInfoAsync(symbol, cancellationToken);
        var (tradeAmount, enterPrice, _) = positionDetails.Data!;
        return (tradeAmount, enterPrice);
    }


    internal async Task TryClosePositionAsync(string symbol,
        SignalType currentAction,
        SignalType actionToTake,
        CancellationToken cancellationToken)
    {
        bool trading = currentAction == SignalType.Buy 
                                || currentAction == SignalType.Sell; 
        bool buyOrSell = actionToTake == SignalType.Buy 
                            || actionToTake == SignalType.Sell;

        if (trading && buyOrSell)
        {
            var amountInTrade = (await GetPositionDetailsAsync(symbol, cancellationToken)).amountInTrade;
            RLogger.AppLog.Information($"CLOSING {currentAction} POSITION....");
            RLogger.AppLog.Information($"BEFORE: {actionToTake}....");

            //close position
            await TryBuyOrSellAsync(
                symbol,
                actionToTake,
                amountInTrade,
                cancellationToken);
            
            //open position
            await TryBuyOrSellAsync(
                symbol,
                actionToTake, 
                amountInTrade,
                cancellationToken);
        }
    }


    public async Task RunAsync(
        string symbol,
        decimal amount,
        decimal profitPercentage,
        int interval,
        CancellationToken cancellationToken)
    {
        TimeSpan profitTimeOut = TimeSpan.FromMinutes(interval);
        SignalType currentPosition = SignalType.Undefined;
        SignalType actionToTake = await GetTargetActionAsync(
            currentPosition,
            symbol,
            interval,
            cancellationToken);

        //first position
        await TryBuyOrSellAsync(
            symbol,
            actionToTake, 
            amount, 
            cancellationToken);

        while (cancellationToken.IsCancellationRequested == false)
        {
            await TryClosePositionAsync(
                symbol,
                currentPosition,
                actionToTake,
                cancellationToken);

            if (actionToTake == SignalType.Buy || actionToTake == SignalType.Sell)
            {
                currentPosition = actionToTake;
            }

            //try to take profit
            using var timeOutCTS = new CancellationTokenSource(profitTimeOut);
            timeOutCTS.Token.Register(() =>
            {
                RLogger.AppLog.Information("PROFIT TIMEOUT REACHED.........");
                RLogger.AppLog.Information("STOPPING PROFIT WATCH....");
            });

            var wsResponse = await futuresHub.WatchTargetProfitAsync(
                symbol,
                (await GetPositionDetailsAsync(symbol, cancellationToken)).enterPrice,
                profitPercentage,
                timeOutCTS.Token);


            (bool takeProfit, decimal percent) result;

            if (wsResponse.Data != default)
            {
                result = wsResponse.Data;
            }
            else
            {
                RLogger.AppLog.Information($"ERROR WATCHING PROFIT WS: {wsResponse.Error}");

                var httpResponse = await WatchProfitHttpAsync(
                    symbol,
                    (await GetPositionDetailsAsync(symbol, cancellationToken)).enterPrice,
                    profitPercentage,
                    timeOutCTS.Token);

                if (httpResponse.Error != null)
                {
                    RLogger.AppLog.Information($"ERROR WATCHING PROFIT HTTP: {httpResponse.Error}");
                    continue;
                }
                result = httpResponse.Data;
            }

            RLogger.AppLog.Information($"takeProfit: {result.takeProfit},  profitPercent: {result.percent}");

            if (result.takeProfit)
            {
                var (amountInTrade, enterPrice) = await GetPositionDetailsAsync(symbol, cancellationToken);
                var profitAmount = amountInTrade * result.percent;

                RLogger.AppLog.Information("TAKING PROFIT....");
                RLogger.AppLog.Information($"PROFIT AMOUNT: {profitAmount}");

                await TryBuyOrSellAsync(symbol, GetOppositeAction(currentPosition), profitAmount, cancellationToken);
            }

            actionToTake = await GetTargetActionAsync(currentPosition, symbol, interval, cancellationToken);
        }
    }



    private async Task<Result<(bool takeProfit, decimal percent)>> WatchProfitHttpAsync(
        string symbol,
        decimal entryPrice,
        decimal targetProfitPercent,
        CancellationToken cancellationToken)
    {
        while (cancellationToken.IsCancellationRequested == false)
        {
            await Task.Delay(5 * 1000, cancellationToken);

            var markPrice = await marketDataService.GetSymbolPriceAsync(symbol, cancellationToken);
            if (markPrice.Error != null)
            {
                RLogger.AppLog.Information($"ERROR WATHING PROFIT.....: {markPrice.Error}");
                continue;
            }

            var currentProfitRatio = (markPrice.Data - entryPrice) / entryPrice;

            if (currentProfitRatio >= targetProfitPercent)
            {
                return new() { Data = (true, currentProfitRatio) };
            }
        }

        return new() { Data = (false, 0) };
    }
}
