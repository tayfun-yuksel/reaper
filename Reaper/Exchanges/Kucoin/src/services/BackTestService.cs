using Reaper.SignalSentinel.Strategies;

namespace Reaper.Exchanges.Kucoin.Services;
public class BackTestService : IBackTestService
{
    public decimal BackTest(decimal tradeAmount, IEnumerable<decimal> prices, string strategy, CancellationToken cancellationToken)
    {
        var pricesList = prices.ToList();
        pricesList.Reverse();
        var shortEntryIndex = 3;
        var t3Values = TilsonT3.CalculateT3([.. pricesList], period: 6, volumeFactor: 1.3m);
        int buySignalCount = 0;
        int sellSignalCount = 0;
        int noSignalCount = 0;
        decimal longEntryPrice = 0;
        decimal shortEntryPrice = 0;
        decimal longAssets = 0;
        decimal shortAssets = 0;
        for (int i = 0; i < pricesList.Count; i++)
        {
            decimal currentPrice = pricesList[i];
            var enterShort = shortEntryIndex <= 0;
            if (pricesList[i] < t3Values[i])
            {
                shortEntryIndex++;
                tradeAmount += shortAssets * (shortEntryPrice - currentPrice);
                longEntryPrice = currentPrice;
                longAssets = tradeAmount / currentPrice;
                shortAssets = 0;
                buySignalCount++;
                // Console.WriteLine($"Buy Signal");
                // Console.WriteLine($"currentPrice: {currentPrice}"); 
                // Console.WriteLine($"tradeAmount: {tradeAmount} ");
            }
            else if (pricesList[i] > t3Values[i])
            {
                shortEntryIndex -= 2;
                tradeAmount += longAssets * (currentPrice - longEntryPrice);
                shortEntryPrice = currentPrice;
                shortAssets = tradeAmount / currentPrice;
                longAssets = 0;
                sellSignalCount++;
                // Console.WriteLine($"Sell Signal");
                // Console.WriteLine($"currentPrice: {currentPrice}"); 
                // Console.WriteLine($"tradeAmount: {tradeAmount} ");
            }
            else
            {
                noSignalCount++;
                // Console.WriteLine($"NO SIGNAL");
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