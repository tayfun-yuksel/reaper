using Reaper.CommonLib.Interfaces;
using Reaper.Exchanges.Kucoin.Interfaces;
using Reaper.SignalSentinel.Strategies;

namespace Reaper.Exchanges.Kucoin.Services;
public class TilsonService(IMarketDataService marketDataService,
    IBrokerService brokerService) : ITilsonService
{
    public async Task RunAsync(string symbol, decimal amount, int interval, CancellationToken cancellationToken)
    {
        async Task buyOrSellAsync()
        {
            var startTime = DateTime.UtcNow.AddMinutes(-(interval * 8)).ToString("dd-MM-yyyy HH:mm");
            var endTime = DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm");
            var last8Prices = await marketDataService.GetKlinesAsync(symbol, startTime, endTime, interval, cancellationToken);
            var t3Values = TilsonT3.CalculateT3([.. last8Prices], period: 6, volumeFactor: 0.5m);

            var t3Last = t3Values.Last();
            var originLast = last8Prices.Last();

            if (t3Last > originLast)
            {
                await brokerService.BuyMarketAsync(symbol, amount, cancellationToken);
            }
            else if (t3Last < originLast)
            {
                await brokerService.SellMarketAsync(symbol, amount, cancellationToken);
            }
        }

        while (cancellationToken.IsCancellationRequested == false)
        {
            await buyOrSellAsync();
            await Task.Delay(TimeSpan.FromMinutes(interval), cancellationToken);
        }
    }
}