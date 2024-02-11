using System.Globalization;
using MathNet.Numerics;
using Reaper.CommonLib.Interfaces;
using Reaper.CommonLib.Utils;
using Reaper.Exchanges.Kucoin.Interfaces;
using Reaper.SignalSentinel.Strategies;

namespace Reaper.Exchanges.Kucoin.Services;
public class Breakout(IMarketDataService marketDataService) : IBreakout
{
    public record RegressionValues(
        double SlopeLows,
        double InterceptLows,
        double SlopeHighs,
        double InterceptHighs,
        double RSquaredLows,
        double RSquaredHighs);

    public enum PivotType { Undefined, Low, High }
    public record Pivot(int Index, PivotType Type,  decimal Value, long Time); 


    public static (double Slope, double Intercept, double RSquared) CalculateRegression(
        List<double> xValues,
        List<double> yValues)
    {
        var (intercept, slope) = Fit.Line([.. xValues], [.. yValues]);
        var rSquared = GoodnessOfFit.RSquared(xValues.Select(x => intercept + x * slope), yValues);

        return (slope, intercept, rSquared); 
    }



    public static Pivot GetPivot(List<FuturesKline> candles, int candle, int window)
    {
        if (candle - window < 0 || candle + window >= candles.Count)
        {
            return default!;
        }

        PivotType type = PivotType.Undefined;
        decimal price = 0m;
        bool low = true;
        bool high = true;
        long time = 0;

        for (int i = candle - window; i <= candle + window; i++)
        {
            if (candles[candle].Low > candles[i].Low)
            {
                low = false;
            }
            if (candles[candle].High < candles[i].High)
            {
                high = false;
            }
        }

        if (low && high)
        {
            type = PivotType.Undefined;
        }
        else if (low)
        {
            type = PivotType.Low;
            price = candles[candle].Low - 1e-3m;
            time = candles[candle].Time;
        }
        else if (high)
        {
            type = PivotType.High;
            price = candles[candle].High + 1e-3m;
            time = candles[candle].Time;
        }

        return new Pivot(candle, type, price, time);
    }


    internal static decimal PositionEntryPoint((bool low, decimal price) lowData,
     (bool high, decimal price) highData)
    {
        if (lowData.low && highData.high)
        {
            return -1;
        }
        else if (lowData.low)
        {
            return lowData.price - 1e-3m;
        }
        else if (highData.high)
        {
            return highData.price + 1e-3m;
        }

        return -1;
    }

    public static RegressionValues CollectChannel(IEnumerable<Pivot?> pivots)
    {
        var lows = pivots.Where(p => p?.Type == PivotType.Low).ToList();
        var highs = pivots.Where(p => p?.Type == PivotType.High).ToList();

        if (lows.Count >= 2 && highs.Count >= 2)
        {
            var (lSlope, lIntercept, lRSquared) = CalculateRegression(
                    lows.Select(x => (double)x.Value).ToList(),
                    lows.Select(x => (double)x.Index).ToList());
            var (hSlope, hIntercept, hRSquared) = CalculateRegression(
                highs.Select(x => (double)x.Value).ToList(),
                highs.Select(x => (double)x.Index).ToList());

            return new(
                lSlope,
                lIntercept,
                hSlope,
                hIntercept,
                lRSquared,
                hRSquared);
        }

        return new(0, 0, 0, 0, 0, 0);
    }



    private static SignalType GetBreakOutSignal(List<FuturesKline> priceData, int candle, int backcandles, int window)
    {
        if (candle - backcandles - window < 0) return 0;

        var candlesToAnalyse = priceData[(candle - backcandles - window).. (candle + 1)];
        var pivots = candlesToAnalyse.Select((c, index) => GetPivot(candlesToAnalyse, index, window));
        var channel = CollectChannel(pivots);

        RLogger.AppLog.Information(@$"SlopeLows: {channel.SlopeLows},
             InterceptLows: {channel.InterceptLows},
             SlopeHighs: {channel.SlopeHighs},
             InterceptHighs: {channel.InterceptHighs},
             rLow: {channel.RSquaredLows},
             rHigh: {channel.RSquaredHighs} \n");


        var prevCandle = priceData[candle - 1];
        var currCandle = priceData[candle];

        var channelLow = (int x) => channel.SlopeLows * x + channel.InterceptLows;
        var channelHigh = (int x) => channel.SlopeHighs * x + channel.InterceptHighs;

        bool breakoutToHigh = (double)prevCandle.High > channelLow(candle - 1)
                    && (double)prevCandle.Close < channelLow(candle - 1)
                    && (double)currCandle.Open < channelLow(candle)
                    && (double)currCandle.Close < channelLow(candle);

        bool breakoutToLow = (double)prevCandle.Low < channelHigh(candle - 1)
                    && (double)currCandle.Close > channelHigh(candle - 1)
                    && (double)currCandle.Open > channelHigh(candle)
                    && (double)currCandle.Close > channelHigh(candle);

        return breakoutToHigh 
            ? SignalType.Buy 
            : breakoutToLow 
                ? SignalType.Sell 
                : SignalType.Undefined;
    }


// plotting-breakout:
//     get price data
//     calculate pivots
//     calculate high regression line
//     calculate low regression line
//     save data to csv file
//     plot data:
//         plot price candlestick chart
//         plot pivot points
//         plot high regression line
//         plot low regression line
    public async Task PrepareDataForPlottingAsync(string symbol, string startTime, int interval)
    {
        var priceData = await marketDataService.GetKlinesAsync(
            symbol,
            startTime,
            endTime: null,
            interval,
            CancellationToken.None);
        
        if (priceData.Error != null){
            return;
        }

        var prices = priceData.Data!.ToList();
        var pivots = prices.Select((c, index) => GetPivot(prices, index, window: 7));

        var channel = CollectChannel(pivots);
        var channelLow = (int x) => channel.SlopeLows * x + channel.InterceptLows;
        var channelHigh = (int x) => channel.SlopeHighs * x + channel.InterceptHighs;

        string file = new FileInfo(Environment.CurrentDirectory + "/Futures/analysis/breakout.csv")
            .FullName.Replace("api", "services");
        using var writer = new StreamWriter(file);
        // Write the header line
        writer.WriteLine("Date,Open,High,Low,Close,ChannelLow,ChannelHigh,PivotType,PivotValue");

        int index = 0;  
        foreach (var candle in prices)
        {
            // Convert date to a culture-invariant format
            string dateString = candle.Time.FromUtcMsToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            // Write the data line
            writer.WriteLine($"{dateString},{candle.Open},{candle.High},{candle.Low},{candle.Close},{channelLow(index)},{channelHigh(index)},{pivots.ElementAt(index)?.Type},{pivots.ElementAt(index)?.Value}");
            index++;
        }

        //todo plot data
        // Plot the data
    }



    public async Task<Result<(string symbol, SignalType signal)>> TryDetectBreakoutAsync(
        string startTime,
        int period)
    {
        var maybeSymbols = await marketDataService.GetSymbolsAsync(CancellationToken.None);
        if (maybeSymbols.Error != null)
        {
            return new() { Error = maybeSymbols.Error };
        }

        foreach (var symbolDetail in maybeSymbols.Data!)
        {
            var maybeCandles = await marketDataService.GetKlinesAsync(
                symbolDetail.Data.Symbol,
                startTime,
                endTime: string.Empty,
                period,
                CancellationToken.None);

            if (maybeCandles.Error != null)
            {
                return new() { Error = maybeCandles.Error };
            }

            var breakoutSignal = GetBreakOutSignal(
                maybeCandles.Data!.ToList(),
                maybeCandles.Data!.Count() - 1,
                backcandles: 10,
                window: 3);

            if (breakoutSignal != SignalType.Undefined)
            {
                return new() { Data = (symbolDetail.Data.Symbol, breakoutSignal)};
            }
        }

        return new() { Data = (string.Empty, SignalType.Undefined) };
    }
}