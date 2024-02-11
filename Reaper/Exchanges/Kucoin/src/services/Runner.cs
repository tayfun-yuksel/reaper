using Reaper.CommonLib.Interfaces;
using Reaper.Exchanges.Kucoin.Interfaces;
using Reaper.SignalSentinel.Strategies;

namespace Reaper.Exchanges.Kucoin.Services;
public class Runner(IMarketDataService marketDataService,
    IBrokerService brokerService,
    IPositionInfoService positionInfoService,
    IFuturesHub futuresHub) : IRunner
{
    internal async Task<SignalType> GetTargetActionAsync(
            SignalType currentPosition,
            string symbol,
            int interval,
            string[] indicators,
            CancellationToken cancellationToken)
    {
        var endTime = DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm");
        var startTime = DateTime.UtcNow
            .AddMinutes(-(interval * 70))
            .ToString("dd-MM-yyyy HH:mm");

        var klines = await marketDataService.GetKlinesAsync(
            symbol,
            startTime,
            endTime,
            interval,
            cancellationToken);

        if (klines.Error != null)
        {
            throw new InvalidOperationException("Error getting klines", klines.Error);
        }

        var prices = klines.Data!.Select(x => x.Close).ToArray();
        var tilsonValues = indicators.Contains("tilson")
            ? Tilson.GetTilsonValues(prices)
            : default;

        var bollingerBands = indicators.Contains("bollinger")
            ? Bollinger.GetBollingerBands(prices)
            : default;

        var macdValues = indicators.Contains("macd")
            ? MACD.GetMACDValues(prices)
            : default;

        var signals = indicators.Select(ind => {
            int lastIndex = klines.Data!.Count() - 1;
            return ind.ToLower() switch
            {
                "tilson" => Tilson.TilsonSignal(
                    lastIndex,
                    prices,
                    tilsonValues!),
                "bollinger" => Bollinger.BollingerSignal(
                    lastIndex,
                    prices,
                    bollingerBands!),
                "macd" => MACD.MACDSignal(lastIndex, macdValues!),
                _ => throw new InvalidOperationException($"Invalid Indicator: {ind}"),
            };
        });

        if (currentPosition != SignalType.Buy && signals.All(x => x == SignalType.Buy))
        {
            RLogger.AppLog.Information($"Buy signal detected for {symbol} at {DateTime.UtcNow}");
            return SignalType.Buy;
        }

        else if (currentPosition != SignalType.Sell && signals.All(x => x == SignalType.Sell))
        {
            RLogger.AppLog.Information($"Sell signal detected for {symbol} at {DateTime.UtcNow}");
            return SignalType.Sell;
        }

        RLogger.AppLog.Information($"Holding {symbol} at {DateTime.UtcNow}");
        return SignalType.Hold;
    }




    public async Task BuyOrSellAsync(
        string symbol,
        SignalType actionToTake,
        decimal amount,
        int leverage,
        CancellationToken cancellationToken)
    {
        if (actionToTake == SignalType.Buy)
        {
            RLogger.AppLog.Information($"BUYING: ..............{symbol} at {DateTime.UtcNow}");
            RLogger.AppLog.Information($"AMOUNT: ..............{amount}\n");
            await brokerService.BuyMarketAsync(symbol, amount, leverage, cancellationToken);
        }
        else if (actionToTake == SignalType.Sell)
        {
            RLogger.AppLog.Information($"SELLING..............{symbol} at {DateTime.UtcNow}");
            RLogger.AppLog.Information($"AMOUNT: .............{amount}\n");
            await brokerService.SellMarketAsync(symbol, amount, leverage, cancellationToken);
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
        int leverage,
        SignalType currentAction,
        SignalType actionToTake,
        CancellationToken cancellationToken)
    {
        bool trading = currentAction == SignalType.Buy
                                || currentAction == SignalType.Sell;
        bool buyOrSell = actionToTake == SignalType.Buy
                            || actionToTake == SignalType.Sell;

        if (!trading || !buyOrSell)
        {
            return;
        }

        var amountInTrade = (await GetPositionDetailsAsync(symbol, cancellationToken)).amountInTrade;
        RLogger.AppLog.Information($"CLOSING {currentAction} POSITION....");
        RLogger.AppLog.Information($"BEFORE: {actionToTake}....");

        //close position
        await BuyOrSellAsync(
            symbol,
            actionToTake,
            amountInTrade,
            leverage,
            cancellationToken);

        //open position
        await BuyOrSellAsync(
            symbol,
            actionToTake,
            amountInTrade,
            leverage,
            cancellationToken);
    }


    private async Task<Result<(bool takeProfit, decimal percent)>> WatchProfitHttpAsync(
        SignalType currentPosition,
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

            var currentProfitRatio = currentPosition == SignalType.Buy
                ? (markPrice.Data! - entryPrice) / entryPrice
                : (entryPrice - markPrice.Data!) / entryPrice;

            if (currentProfitRatio >= targetProfitPercent)
            {
                return new() { Data = (true, currentProfitRatio) };
            }
        }

        return new() { Data = (false, 0) };
    }


    public async Task RunAsync(
        string symbol,
        decimal amount,
        int leverage,
        decimal profitPercentage,
        int interval,
        string[] indicators,
        CancellationToken cancellationToken)
    {
        TimeSpan profitTimeOut = TimeSpan.FromMinutes(interval);
        SignalType currentPosition = SignalType.Undefined;

        SignalType actionToTake = await GetTargetActionAsync(
            currentPosition,
            symbol,
            interval,
            indicators,
            cancellationToken);

        //first position
        await BuyOrSellAsync(
            symbol,
            actionToTake, 
            amount, 
            leverage,
            cancellationToken);

        using var _ = cancellationToken.Register(() =>
        {
            RLogger.AppLog.Information("CANCELLATION REQUESTED....");
            RLogger.AppLog.Information("STOPPING STRATEGY....");
            RLogger.AppLog.Information("CLOSING POSITION....");
            TryClosePositionAsync(
                symbol,
                leverage,
                currentPosition,
                actionToTake,
                cancellationToken).Wait();
        });

        while (cancellationToken.IsCancellationRequested == false)
        {
            await TryClosePositionAsync(
                symbol,
                leverage,
                currentPosition,
                actionToTake,
                cancellationToken);

            if (actionToTake == SignalType.Buy || actionToTake == SignalType.Sell)
            {
                currentPosition = actionToTake;
            }

            //try to take profit
            using var timeOutCTS = new CancellationTokenSource(profitTimeOut);
            using var timeoutRegistration = timeOutCTS.Token.Register(() =>
            {
                RLogger.AppLog.Information("PROFIT TIMEOUT REACHED.........");
                RLogger.AppLog.Information("STOPPING PROFIT WATCH....");
            });

            var profitResponse = await futuresHub.WatchTargetProfitAsync(
                currentPosition,
                symbol,
                (await GetPositionDetailsAsync(symbol, cancellationToken)).enterPrice,
                profitPercentage,
                timeOutCTS.Token);


            (bool takeProfit, decimal percent) profit;

            if (profitResponse.Data != default)
            {
                profit = profitResponse.Data;
            }
            else
            {
                RLogger.AppLog.Information($"ERROR WATCHING PROFIT WS: {profitResponse.Error}");
                RLogger.AppLog.Information("FALLING BACK TO HTTP....");

                var httpResponse = await WatchProfitHttpAsync(
                    currentPosition,
                    symbol,
                    (await GetPositionDetailsAsync(symbol, cancellationToken)).enterPrice,
                    profitPercentage,
                    timeOutCTS.Token);

                if (httpResponse.Error != null)
                {
                    RLogger.AppLog.Information($"ERROR WATCHING PROFIT HTTP: {httpResponse.Error}");
                    RLogger.AppLog.Information("WAITING FOR NEXT INTERVAL....");
                    await Task.Delay(interval, timeOutCTS.Token);
                    continue;
                }
                profit = httpResponse.Data;
            }

            RLogger.AppLog.Information($"takeProfit: {profit.takeProfit},  profitPercent: {profit.percent}");

            if (profit.takeProfit)
            {
                var (amountInTrade, enterPrice) = await GetPositionDetailsAsync(symbol, cancellationToken);
                var profitAmount = amountInTrade * profit.percent;

                RLogger.AppLog.Information("TAKING PROFIT....");
                RLogger.AppLog.Information($"PROFIT AMOUNT: {profitAmount}");

                await BuyOrSellAsync(
                    symbol,
                    GetOppositeAction(currentPosition),
                    profitAmount,
                    leverage,
                    cancellationToken);
            }

            actionToTake = await GetTargetActionAsync(
                currentPosition,
                symbol,
                interval,
                indicators,
                cancellationToken);
        }
    }



}
