using Reaper.CommonLib.Interfaces;
using Reaper.Exchanges.Kucoin.Interfaces;
using Reaper.SignalSentinel.Strategies;

namespace Reaper.Exchanges.Kucoin.Services;
public class TilsonService(IMarketDataService marketDataService,
    IBrokerService brokerService,
    IPositionInfoService positionService,
    IFuturesHub futuresHub) : ITilsonService
{
    internal async Task<SignalType> GetBuyOrSellSignalAsync(
            SignalType side,
            string symbol,
            int interval,
            CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow.AddMinutes(-(interval * 50)).ToString("dd-MM-yyyy HH:mm");
        var endTime = DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm");
        var last8Prices = await marketDataService.GetKlinesAsync(symbol, startTime, endTime, interval, cancellationToken);
        var t3Values = TilsonT3.CalculateT3([.. last8Prices], period: 6, volumeFactor: 0.5m);

        var t3Last = t3Values.Last();
        var originLast = last8Prices.Last();

        if (side != SignalType.Buy && t3Last > originLast)
        {
            return SignalType.Buy;
        }
        else if (side != SignalType.Sell && t3Last < originLast)
        {
            return SignalType.Sell;
        }
        Console.WriteLine($"Holding {symbol} at {DateTime.UtcNow}");
        return SignalType.Hold;
    }




    public async Task RunAsync(
        string symbol,
        decimal amount,
        decimal profitPercentage,
        int interval,
        CancellationToken cancellationToken)
    {
        TimeSpan profitTimeOut = TimeSpan.FromMinutes(interval);

        var buyOrSellFn = async (SignalType side, decimal posAmount) => {
            if (side == SignalType.Buy)
                await brokerService.SellMarketAsync(symbol, posAmount, cancellationToken);
            else if (side == SignalType.Sell)
                await brokerService.BuyMarketAsync(symbol, posAmount, cancellationToken);
        };
        

        var getCloseSignalFn = (SignalType side) =>
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
        };


        var positionSignal = await GetBuyOrSellSignalAsync(
            SignalType.Undefined,
            symbol,
            interval,
            cancellationToken);

        await buyOrSellFn(positionSignal, amount);

        while (cancellationToken.IsCancellationRequested == false)
        {
            //update amount info based on current market data
            (amount, _) = await positionService.GetPositionInfoAsync(symbol, cancellationToken);
            var targetProfit = amount * profitPercentage;
            var (takeProfit, realizedPnl) = await futuresHub.WatchTargetProfitAsync(targetProfit, profitTimeOut, symbol);

            if (takeProfit)
            {
                Console.WriteLine($"Realized Pnl: {realizedPnl}");
                Console.WriteLine("Taking profit....");
                await buyOrSellFn(getCloseSignalFn(positionSignal), realizedPnl);
            }

            positionSignal = await GetBuyOrSellSignalAsync(
                positionSignal,
                symbol,
                interval,
                cancellationToken);

            if (positionSignal == SignalType.Hold)
            {
                Console.WriteLine($"Holding {symbol} at {DateTime.UtcNow}");
                continue;
            }

            //close position 
            await buyOrSellFn(getCloseSignalFn(positionSignal), amount);
            //open position
            await buyOrSellFn(positionSignal, amount);
        }
    }
}