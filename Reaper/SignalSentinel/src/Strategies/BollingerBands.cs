using System;

namespace Reaper.SignalSentinel.Strategies;
public static class BollingerBands
{
    private static decimal SMA(decimal[] prices, int period, int index)
    {
        if (index - period + 1 < 0) throw new ArgumentException("Not enough data to calculate SMA");

        decimal sum = 0.0m;
        for (int i = index; i > index - period; i--)
        {
            sum += prices[i];
        }
        return sum / period;
    }

    private static decimal StandardDeviation(decimal[] prices, int period, int index)
    {
        decimal mean = SMA(prices, period, index);
        decimal sumOfSquares = 0.0m;
        for (int i = index; i > index - period; i--)
        {
            decimal diff = prices[i] - mean;
            sumOfSquares += diff * diff;
        }
        return (decimal)Math.Sqrt((double)(sumOfSquares / period));
    }




    public static (decimal[] upperBand, decimal[] middleBand, decimal[] lowerBand) 
        CalculateBollingerBands(decimal[] prices, int period, decimal standardDeviationMultiplier)
    {
        if (prices.Length < period) 
        {
            throw new ArgumentException("Not enough data to calculate Bollinger Bands");
        }

        decimal[] upperBand = new decimal[prices.Length];
        decimal[] middleBand = new decimal[prices.Length];
        decimal[] lowerBand = new decimal[prices.Length];

        for (int i = period - 1; i < prices.Length; i++)
        {
            decimal sma = SMA(prices, period, i);
            decimal stdDev = StandardDeviation(prices, period, i);

            upperBand[i] = sma + stdDev * standardDeviationMultiplier;
            middleBand[i] = sma;
            lowerBand[i] = sma - stdDev * standardDeviationMultiplier;
        }

        return (upperBand, middleBand, lowerBand);
    }
}

