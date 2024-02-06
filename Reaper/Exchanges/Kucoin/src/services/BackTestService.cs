using System.Globalization;
using Reaper.SignalSentinel.Strategies;

namespace Reaper.Exchanges.Kucoin.Services;
public class BackTestService(IMarketDataService marketDataService) : IBackTestService
{
    internal static decimal GetProfitAmount(decimal entryPrice, decimal currentPrice, decimal assests, SignalType position)
    {
        if (position != SignalType.Buy && position != SignalType.Sell)
        {
            return 0;
        }

        if (position == SignalType.Buy)
        {
            var longProfit = (currentPrice - entryPrice) * assests;
            RLogger.AppLog.Information($"Realized Profit From Long: {longProfit}");
            return longProfit;
        }
        var shortProfit = (entryPrice - currentPrice) * assests;
        RLogger.AppLog.Information($"Realized Profit From Short: {shortProfit}");
        return shortProfit;
    }


    internal async Task<List<decimal>> GetKlinesAsync(string symbol, string startTime, string? endTime, int interval, CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var getTimeRange = (string start, int interval) =>
        {
            var fromResult = start.ToUtcEpochMs();
            if (fromResult.Error != null)
            {
                //todo: handle result<>
                throw new InvalidOperationException("Error getting klines", fromResult.Error);
            }

            int klinesLimit = 200;
            long from = fromResult.Data!;
            long msMultiplier = 60 * 1000;
            long range = interval * klinesLimit * msMultiplier;
            long to = from + range;

            if (to > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            {
                to = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            var fromStr = from.FromUtcMsToLocalTime().ToString("dd-MM-yyyy HH:mm");
            var endStr = to.FromUtcMsToLocalTime().ToString("dd-MM-yyyy HH:mm");
            return (fromStr, endStr);
        };

        List<decimal> prices = [];
        var (from, to) = getTimeRange(startTime, interval);

        while (true)
        {
            RLogger.AppLog.Information($"Getting klines from {from} to {to}");
            var klinesResult = await marketDataService
                .GetKlinesAsync(symbol, from, to, interval, cancellationToken);

            if (klinesResult.Error != null)
            {
                throw new InvalidOperationException("Error getting klines", klinesResult.Error);
            }

            from = to;
            (from, to) = getTimeRange(from, interval);

            prices.AddRange(klinesResult.Data!);

            if (!klinesResult.Data!.Any() || DateTime.ParseExact(
                    to,
                    "dd-MM-yyyy HH:mm",
                    CultureInfo.InvariantCulture) >= now)
            {
                break;
            }
        }
        return prices;
    }




    internal static decimal CalculateTradeAmount(string symbol, int startIndex, decimal tradeAmount,
         decimal[] originalPriceList, Func<int, SignalType> signalAction)
    {
        if (originalPriceList.Length == 0)
        {
            RLogger.AppLog.Error("NO PRICE DATA");
            return tradeAmount;
        }

        SignalType position = SignalType.Undefined;
        int noSignalCount = 0;
        decimal entryPrice = 0;
        decimal assests = 0; 

        for (int i = startIndex; i < originalPriceList.Length; i++)
        {
            decimal currentPrice = originalPriceList[i];
            var actionToTake = signalAction(i);

            if (position != SignalType.Buy && actionToTake == SignalType.Buy)
            {
                RLogger.AppLog.Information($"........................BUYING {symbol.ToUpper()}...................");

                assests = tradeAmount / currentPrice;
                tradeAmount += GetProfitAmount(entryPrice, currentPrice, assests, position);
                entryPrice = currentPrice;
                position = SignalType.Buy;
            }
            else if (position != SignalType.Sell && actionToTake == SignalType.Sell)
            {
                RLogger.AppLog.Information($"........................SELLING {symbol.ToUpper()}...................");

                assests = tradeAmount / currentPrice;
                tradeAmount += GetProfitAmount(entryPrice, currentPrice, assests, position);
                entryPrice = currentPrice;
                position = SignalType.Sell;
            }
            else
            {
                RLogger.AppLog.Information($".......................HOLDING {symbol.ToUpper()} AT {DateTime.UtcNow}.....");
                RLogger.AppLog.Information($"Current Price: {currentPrice}");
                RLogger.AppLog.Information($"Assets: {assests}");
                RLogger.AppLog.Information($"Current Balance: {assests * currentPrice} \n");
                noSignalCount++;
            }

        }

        RLogger.AppLog.Information($"Final: {tradeAmount}");
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

        var amount = CalculateTradeAmount(symbol, period, tradeAmount, pricesList, signalAction);
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

        var amount = CalculateTradeAmount(symbol, longPeriod, tradeAmount, pricesList, signalAction);
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
            return SignalType.Hold;
        };

        var amount = CalculateTradeAmount(symbol, period, tradeAmount, [.. pricesList], signalAction);
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
        var amount = CalculateTradeAmount(symbol, period, tradeAmount, pricesList, signalAction);
        return amount;
    } 

}