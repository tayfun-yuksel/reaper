using System;
namespace Reaper.SignalSentinel.Strategies;
public static class TilsonT3
{
    private static decimal EMA(decimal[] prices, int period, int index)
    {
        if (index - period + 1 < 0) throw new ArgumentException("Not enough data to calculate EMA");

        decimal k = (decimal)2.0 / (period + 1);
        decimal ema = prices[index];
        for (int i = index - 1; i >= index - period + 1; i--)
        {
            ema = prices[i] * k + ema * (1 - k);
        }
        return ema;
    }

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


    // prices, 5, 0.5
    public static decimal[] CalculateT3_Version2(decimal[] prices, int length, decimal volumeFactor)
    {
        int emaLength = length;

        decimal[] e1 = new decimal[prices.Length];
        decimal[] e2 = new decimal[prices.Length];
        decimal[] e3 = new decimal[prices.Length];
        decimal[] e4 = new decimal[prices.Length];
        decimal[] e5 = new decimal[prices.Length];
        decimal[] e6 = new decimal[prices.Length];
        decimal[] t3 = new decimal[prices.Length];

        for (int i = 0; i < prices.Length; i++)
        {
            if (i == 0)
            {
                // For the first element, EMA is same as the price
                e1[i] = prices[i];
                e2[i] = prices[i];
                e3[i] = prices[i];
                e4[i] = prices[i];
                e5[i] = prices[i];
                e6[i] = prices[i];
                t3[i] = prices[i];
            }
            else
            {
                // Calculate EMA at each step
                decimal c1 = 2.0m / (emaLength + 1);
                decimal c2 = 1 - c1;

                e1[i] = c1 * prices[i] + c2 * e1[i - 1];
                e2[i] = c1 * e1[i] + c2 * e2[i - 1];
                e3[i] = c1 * e2[i] + c2 * e3[i - 1];
                e4[i] = c1 * e3[i] + c2 * e4[i - 1];
                e5[i] = c1 * e4[i] + c2 * e5[i - 1];
                e6[i] = c1 * e5[i] + c2 * e6[i - 1];

                // Calculate T3
                decimal v = volumeFactor * volumeFactor;
                t3[i] = (1 - v) * e6[i] + v * t3[i - 1];
            }
        }
        return t3;
    }

}
