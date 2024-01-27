using Reaper.SignalSentinel.Strategies;

namespace Reaper.Exchanges.Kucoin.Services;
public class BackTestService(IMarketDataService marketDataService) : IBackTestService
{
    public async Task<decimal> BackTestAsync(string symbol, string startTime,
        string? endTime,
        int interval,
        decimal tradeAmount,
        string strategy,
        decimal? volumeFactor,
        CancellationToken cancellationToken)
    {
        volumeFactor ??= 0.7m;
        var getFromAndToTimeFn = (string startTime, int interval) =>
        {
            int klinesLimit = 200;
            long from = startTime.ToUtcEpochMs(); 
            long msMultiplier = 60 * 1000;
            long range = interval * klinesLimit * msMultiplier;
            long to = from + range;

            if (to > DateTime.UtcNow.Ticks)
            {
                to = DateTime.UtcNow.Ticks;
            }
            var fromStr = from.FromUtcMsToLocalTime().ToString("dd-MM-yyyy HH:mm");
            var endStr = to.FromUtcMsToLocalTime().ToString("dd-MM-yyyy HH:mm");
            return (fromStr, endStr);
        };

        List<decimal> prices = [];
        var (from, to) = getFromAndToTimeFn(startTime, interval);
        while (true)
        {
            var klines = await marketDataService.GetKlinesAsync(symbol, from, to, interval, cancellationToken);
            from = to;
            (from, to) = getFromAndToTimeFn(from, interval);

            prices.AddRange(klines);
            if (!klines.Any())
            {
                break;
            }
        }

        return strategy.ToLower() switch
        {
            "tilsont3" => HandleTilsonT3(tradeAmount, prices, (decimal)volumeFactor, cancellationToken),
            _ => 0
        };
    }


    public static decimal HandleTilsonT3(decimal tradeAmount, IEnumerable<decimal> prices, decimal volumeFactor, CancellationToken cancellationToken)
    {
        var signalType = SignalType.Undefined;
        var pricesList = prices.ToList();
        pricesList.Reverse();

        int period = 6;
        var t3Values = TilsonT3.CalculateT3([.. pricesList], period, volumeFactor);

        int buySignalCount = 0;
        int sellSignalCount = 0;
        int noSignalCount = 0;
        decimal longEntryPrice = 0;
        decimal shortEntryPrice = 0;

        for (int i = period - 1; i < pricesList.Count; i++)
        {
            decimal currentPrice = pricesList[i];
            decimal assests = tradeAmount / currentPrice;
            decimal realizedProfit;
            if (signalType != SignalType.Buy && pricesList[i] < t3Values[i])
            {
                if (signalType == SignalType.Sell)
                {
                    realizedProfit = (shortEntryPrice - currentPrice) * assests;
                    tradeAmount += realizedProfit;
                    Console.WriteLine($"Realized Profit: {realizedProfit}");
                }

                longEntryPrice = currentPrice;
                signalType = SignalType.Buy;
                buySignalCount++;
                Console.WriteLine($"Buy Signal");
                // Console.WriteLine($"currentPrice: {currentPrice}"); 
                // Console.WriteLine($"tradeAmount: {tradeAmount} ");
            }
            else if (signalType != SignalType.Sell && pricesList[i] > t3Values[i])
            {
                if (signalType == SignalType.Buy)
                {
                    realizedProfit = (currentPrice - longEntryPrice) * assests;
                    tradeAmount += realizedProfit;
                    Console.WriteLine($"Realized Profit: {realizedProfit}");
                }
                shortEntryPrice = currentPrice;
                signalType = SignalType.Sell;
                sellSignalCount++;
                Console.WriteLine($"Sell Signal");
                // Console.WriteLine($"currentPrice: {currentPrice}"); 
                // Console.WriteLine($"tradeAmount: {tradeAmount} ");
            }
            else
            {
                noSignalCount++;
                Console.WriteLine($"NO SIGNAL");
            }
            // Console.WriteLine($"Trade Amount: {tradeAmount}"); 
            // Console.WriteLine($"longAssets: {longAssets} ");
            // Console.WriteLine($"shortAssets: {shortAssets} ");
            // Console.WriteLine("-------------------------------------\n\n\n");
        }

        Console.WriteLine($"Final: {tradeAmount}"); 
        return  tradeAmount;
    }
}