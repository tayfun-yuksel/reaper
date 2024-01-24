using System;
namespace Reaper.SignalSentinel.Strategies;
public class TilsonT3
{
    // Function to calculate EMA (Exponential Moving Average)
    private static decimal EMA(decimal[] prices, int period, int index)
    {
        decimal k = (decimal)2.0 / (period + 1);
        decimal ema = prices[index];
        for (int i = index - 1; i >= index - period + 1; i--)
        {
            ema = prices[i] * k + ema * (1 - k);
        }
        return ema;
    }

    // Function to calculate T3
    public static decimal[] CalculateT3(decimal[] prices, int period, decimal volumeFactor)
    {
        int length = prices.Length;
        decimal[] e1 = new decimal[length];
        decimal[] e2 = new decimal[length];
        decimal[] e3 = new decimal[length];
        decimal[] e4 = new decimal[length];
        decimal[] e5 = new decimal[length];
        decimal[] e6 = new decimal[length];
        decimal[] t3 = new decimal[length];

        for (int i = period - 1; i < length; i++)
        {
            e1[i] = EMA(prices, period, i);
            e2[i] = EMA(e1, period, i);
            e3[i] = EMA(e2, period, i);
            e4[i] = EMA(e3, period, i);
            e5[i] = EMA(e4, period, i);
            e6[i] = EMA(e5, period, i);

            decimal c1 = -volumeFactor * volumeFactor * volumeFactor;
            decimal c2 = 3 * volumeFactor * volumeFactor + 3 * volumeFactor * volumeFactor * volumeFactor;
            decimal c3 = -6 * volumeFactor * volumeFactor - 3 * volumeFactor - 3 * volumeFactor * volumeFactor * volumeFactor;
            decimal c4 = 1 + 3 * volumeFactor + volumeFactor * volumeFactor * volumeFactor + 3 * volumeFactor * volumeFactor;

            t3[i] = c1 * e6[i] + c2 * e5[i] + c3 * e4[i] + c4 * e3[i];
        }

        return t3;
    }

   
}
