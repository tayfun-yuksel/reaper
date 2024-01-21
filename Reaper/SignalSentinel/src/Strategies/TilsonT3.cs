using System;
namespace Reaper.SignalSentinel.Strategies;
public class TilsonT3
{
    // Function to calculate EMA (Exponential Moving Average)
    private double EMA(double[] prices, int period, int index)
    {
        double k = 2.0 / (period + 1);
        double ema = prices[index];
        for (int i = index - 1; i >= index - period + 1; i--)
        {
            ema = prices[i] * k + ema * (1 - k);
        }
        return ema;
    }

    // Function to calculate T3
    public double[] CalculateT3(double[] prices, int period, double volumeFactor)
    {
        int length = prices.Length;
        double[] e1 = new double[length];
        double[] e2 = new double[length];
        double[] e3 = new double[length];
        double[] e4 = new double[length];
        double[] e5 = new double[length];
        double[] e6 = new double[length];
        double[] t3 = new double[length];

        for (int i = period - 1; i < length; i++)
        {
            e1[i] = EMA(prices, period, i);
            e2[i] = EMA(e1, period, i);
            e3[i] = EMA(e2, period, i);
            e4[i] = EMA(e3, period, i);
            e5[i] = EMA(e4, period, i);
            e6[i] = EMA(e5, period, i);

            double c1 = -volumeFactor * volumeFactor * volumeFactor;
            double c2 = 3 * volumeFactor * volumeFactor + 3 * volumeFactor * volumeFactor * volumeFactor;
            double c3 = -6 * volumeFactor * volumeFactor - 3 * volumeFactor - 3 * volumeFactor * volumeFactor * volumeFactor;
            double c4 = 1 + 3 * volumeFactor + volumeFactor * volumeFactor * volumeFactor + 3 * volumeFactor * volumeFactor;

            t3[i] = c1 * e6[i] + c2 * e5[i] + c3 * e4[i] + c4 * e3[i];
        }

        return t3;
    }
}
