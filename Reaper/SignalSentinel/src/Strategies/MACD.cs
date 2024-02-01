using System;

namespace Reaper.SignalSentinel.Strategies;
public static class MACD
{
    private static decimal EMA(decimal[] prices, int period, int index)
    {
        if (index - period + 1 < 0) throw new ArgumentException("Not enough data to calculate EMA");

        decimal multiplier = 2.0m / (period + 1);
        decimal ema = prices[index];
        for (int i = index - 1; i >= index - period + 1; i--)
        {
            ema = (prices[i] - ema) * multiplier + ema;
        }
        return ema;
    }

    
    //12, 26, 9
    public static (decimal[] macdLine, decimal[] signalLine) CalculateMACD(decimal[] prices, int fastLength, int slowLength, int signalSmoothing)
    {
        decimal[] macdLine = new decimal[prices.Length];
        decimal[] signalLine = new decimal[prices.Length];

        for (int i = slowLength - 1; i < prices.Length; i++)
        {
            decimal fastEMA = EMA(prices, fastLength, i);
            decimal slowEMA = EMA(prices, slowLength, i);

            macdLine[i] = fastEMA - slowEMA;
            signalLine[i] = EMA(macdLine, signalSmoothing, i);
        }

        return (macdLine, signalLine);
    }
}
