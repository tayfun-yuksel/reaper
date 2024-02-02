using Reaper.SignalSentinel.Strategies;

namespace Reaper.Exchanges.Kucoin.Services;
public class BackTestService(IMarketDataService marketDataService) : IBackTestService
{
    internal static decimal GetShortPositionProfit(decimal shortEntryPrice, decimal currentPrice, decimal assests)
    {
        var realizedProfit = (shortEntryPrice - currentPrice) * assests;
        Console.WriteLine($"Realized Profit From Short: {realizedProfit}");
        return realizedProfit;
    }

    internal static decimal GetLongPositionProfit(decimal longEntryPrice, decimal currentPrice, decimal assests)
    {
        var realizedProfit = (currentPrice - longEntryPrice) * assests;
        Console.WriteLine($"Realized Profit From Long: {realizedProfit}");
        return realizedProfit;
    }

    internal async Task<List<decimal>> GetKlinesAsync(string symbol, string startTime, string? endTime, int interval, CancellationToken cancellationToken)
    {
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
            var klinesResult = await marketDataService.GetKlinesAsync(symbol, from, to, interval, cancellationToken);
            if (klinesResult.Error != null)
            {
                throw new InvalidOperationException("Error getting klines", klinesResult.Error);
            }

            from = to;
            (from, to) = getFromAndToTimeFn(from, interval);

            prices.AddRange(klinesResult.Data!);
            if (!klinesResult.Data!.Any())
            {
                break;
            }
        }
        return prices;
    }




    internal static decimal CalculateTradeAmount(int startIndex, decimal tradeAmount,
         decimal[] originalPriceList, Func<int, SignalType> signalAction)
    {
        SignalType position = SignalType.Undefined;
        int noSignalCount = 0;
        decimal entryPrice = 0;


        for (int i = startIndex; i < originalPriceList.Length; i++)
        {
            decimal currentPrice = originalPriceList[i];
            decimal assests = tradeAmount / currentPrice;

            var actionToTake = signalAction(i);

            if (position != SignalType.Buy && actionToTake == SignalType.Buy)
            {
                if (position == SignalType.Sell)
                {
                    tradeAmount += GetShortPositionProfit(entryPrice, currentPrice, assests);
                }
                entryPrice = currentPrice;
                position = SignalType.Buy;
                Console.WriteLine($"Buy Signal");
            }
            else if (position != SignalType.Sell && actionToTake == SignalType.Sell)
            {
                if (position == SignalType.Buy)
                {
                    tradeAmount += GetLongPositionProfit(entryPrice, currentPrice, assests);
                }
                entryPrice = currentPrice;
                position = SignalType.Sell;
                Console.WriteLine($"Sell Signal");
            }
            else
            {
                noSignalCount++;
                Console.WriteLine($"NO SIGNAL");
            }
        }

        Console.WriteLine($"Final: {tradeAmount}");
        return tradeAmount;
    }





    public async Task<decimal> BollingerBandsAsync(string symbol, string startTime, string? endTime, int interval,
                                            decimal tradeAmount, int period, decimal deviationMultiplier,
                                            CancellationToken cancellationToken)
    {
        var prices = await GetKlinesAsync(symbol, startTime, endTime, interval, cancellationToken);
        var pricesList = prices.ToArray();
        pricesList = pricesList.Reverse().ToArray();
        var (upperBand, middleBand, lowerBand) = BollingerBands.CalculateBollingerBands(pricesList, period, deviationMultiplier);

        SignalType signalAction(int index)
        {
            var currentPrice = pricesList[index];
            var deltaUpper = upperBand[index] - currentPrice;
            var deltaLower = currentPrice - lowerBand[index];
            if(deltaUpper > deltaLower)
            {
                return SignalType.Buy;
            }
            else if(deltaUpper < deltaLower)
            {
                return SignalType.Sell;
            }
            return SignalType.Hold;
        };

        var amount = CalculateTradeAmount(period, tradeAmount, pricesList, signalAction);
        return amount;

    }



    public async Task<decimal> MACDAsync(string symbol,
                                         string startTime,
                                         string? endTime,
                                         int interval,
                                         decimal tradeAmount,
                                         int shortPeriod,
                                         int longPeriod,
                                         int smoothLine,
                                         CancellationToken cancellationToken)
    {
        var prices = await GetKlinesAsync(symbol, startTime, endTime, interval, cancellationToken);
        var pricesList = prices.ToArray();
        pricesList = pricesList.Reverse().ToArray();
        var (macdLines, signalLines) = MACD.CalculateMACD(pricesList, shortPeriod, longPeriod, smoothLine);

        SignalType signalAction(int index)
        {
            int buySignalCount = 0;
            int sellSignalCount = 0;
            for (int i = index - shortPeriod; i < index; i++)
            {
                if (macdLines[i] > signalLines[i])
                {
                    buySignalCount++;
                }
                else if (macdLines[i] < signalLines[i])
                {
                    sellSignalCount++;
                }
            }

            if (buySignalCount > sellSignalCount)
            {
                return SignalType.Buy;
            }
            else if (buySignalCount < sellSignalCount)
            {
                return SignalType.Sell;
            }

            return SignalType.Hold;
        }

        var amount = CalculateTradeAmount(longPeriod, tradeAmount, pricesList, signalAction);
        return amount;
    }





    public async Task<decimal> TilsonT3Async(string symbol, string startTime,
        string? endTime,
        int interval,
        decimal tradeAmount,
        decimal volumeFactor,
        CancellationToken cancellationToken)
    {
        var prices = await GetKlinesAsync(symbol, startTime, endTime, interval, cancellationToken);
        var pricesList = prices.ToList();
        pricesList.Reverse();
        int period = 6;
        var t3Values = TilsonT3.CalculateT3([.. pricesList], period, volumeFactor);

        SignalType signalAction(int index) 
        {

            if (t3Values[index] > pricesList[index])
            {
                return SignalType.Buy;
            }
            else if (t3Values[index] < pricesList[index])
            {
                return SignalType.Sell;
            }

            // int buySignalCount = 0;
            // int sellSignalCount = 0;
            // for (int i = index - 3; i < index; i++)
            // {
            //     if (t3Values[i] > pricesList[i])
            //     {
            //         buySignalCount++;
            //     }
            //     else if (t3Values[i] < pricesList[i])
            //     {
            //         sellSignalCount++;
            //     }
            // }

            // if (buySignalCount > sellSignalCount)
            // {
            //     return SignalType.Buy;
            // }
            // else if (buySignalCount < sellSignalCount)
            // {
            //     return SignalType.Sell;
            // }

            return SignalType.Hold;
        
        };

        var amount = CalculateTradeAmount(period, tradeAmount, [.. pricesList], signalAction);
        return amount;
    }



    public async Task<decimal> BollingerBandsAndTilsonT3Async(string symbol, string startTime, string? endTime, int interval,
                                            decimal tradeAmount, decimal tilsonVolumeFactor, int tilsonPeriod,
                                            int bollingerPeriod, decimal deviationMultiplier,
                                            CancellationToken cancellationToken)
    {
        var prices = await GetKlinesAsync(symbol, startTime, endTime, interval, cancellationToken);
        var pricesList = prices.ToArray();
        pricesList = pricesList.Reverse().ToArray();


        var tilsonValues = TilsonT3.CalculateT3(pricesList, tilsonPeriod, tilsonVolumeFactor);
        var (upperBand, middleBand, lowerBand) = BollingerBands.CalculateBollingerBands(pricesList, bollingerPeriod,
                                                                                        deviationMultiplier);


        SignalType bollingerSignal(int index)
        {
            var currentPrice = pricesList[index];
            var deltaUpper = upperBand[index] - currentPrice;
            var deltaLower = currentPrice - lowerBand[index];
            if (deltaUpper > deltaLower)
            {
                return SignalType.Buy;
            }
            else if (deltaUpper < deltaLower)
            {
                return SignalType.Sell;
            }
            return SignalType.Hold;
        };


        SignalType tilsonSignal(int index)
        {
            if (tilsonValues[index] > pricesList[index])
            {
                return SignalType.Buy;
            }
            else if (tilsonValues[index] < pricesList[index])
            {
                return SignalType.Sell;
            }

            return SignalType.Hold;
        };



        var signalAction = (int index) =>
        {
            var bollingerSignalType = bollingerSignal(index);
            var tilsonSignalType = tilsonSignal(index);

            if (bollingerSignalType == SignalType.Buy && tilsonSignalType == SignalType.Buy)
            {
                return SignalType.Buy;
            }
            else if (bollingerSignalType == SignalType.Sell && tilsonSignalType == SignalType.Sell)
            {
                return SignalType.Sell;
            }

            return SignalType.Hold;
        };

        var period = tilsonPeriod > bollingerPeriod ? tilsonPeriod : bollingerPeriod;
        var amount = CalculateTradeAmount(period, tradeAmount, pricesList, signalAction);
        return amount;
    } 

}