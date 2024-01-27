using Reaper.CommonLib.Interfaces;
using Reaper.Exchanges.Kucoin.Interfaces;
using Reaper.SignalSentinel.Strategies;

namespace Reaper.Exchanges.Kucoin.Services;
public class TilsonService(IMarketDataService marketDataService,
    IBrokerService brokerService,
    IPositionService positionService) : ITilsonService
{
    public async Task RunAsync(string symbol, decimal amount, int interval, CancellationToken cancellationToken)
    {
        var signalType = SignalType.Undefined;
        var buyOrSellFn = async(string symbol, decimal amount, int interval, CancellationToken cancellationToken) =>
        {
            var startTime = DateTime.UtcNow.AddMinutes(-(interval * 50)).ToString("dd-MM-yyyy HH:mm");
            var endTime = DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm");
            var last8Prices = await marketDataService.GetKlinesAsync(symbol, startTime, endTime, interval, cancellationToken);
            var t3Values = TilsonT3.CalculateT3([.. last8Prices], period: 6, volumeFactor: 0.314m);

            var t3Last = t3Values.Last();
            var originLast = last8Prices.Last();

            if (signalType != SignalType.Buy && t3Last > originLast)
            {
                //close sell position
                if (signalType == SignalType.Sell)
                {
                    Console.WriteLine("closing position");
                    await brokerService.BuyMarketAsync(symbol, amount, cancellationToken);
                }

                Console.WriteLine($"Long position {symbol} at {DateTime.UtcNow}");
                await brokerService.BuyMarketAsync(symbol, amount, cancellationToken);
                signalType = SignalType.Buy;
            }
            else if (signalType != SignalType.Sell && t3Last < originLast)
            {
                //close buy position
                if (signalType == SignalType.Buy)
                {
                    Console.WriteLine("closing long position");
                    await brokerService.SellMarketAsync(symbol, amount, cancellationToken);
                }

                Console.WriteLine($"Short position {symbol} at {DateTime.UtcNow}");
                await brokerService.SellMarketAsync(symbol, amount, cancellationToken);
                signalType = SignalType.Sell;
            }
            else
            {
                Console.WriteLine($"Holding {symbol} at {DateTime.UtcNow}");
            }
            return signalType;
        };

        while (cancellationToken.IsCancellationRequested == false)
        {
            signalType = await buyOrSellFn(symbol, amount, interval, cancellationToken);
            await Task.Delay(TimeSpan.FromMinutes(interval), cancellationToken);
            amount = await positionService.GetPositionAmountAsync(symbol, cancellationToken);
        }
    }
}