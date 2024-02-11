using System.CodeDom.Compiler;
using System.Globalization;
using MathNet.Numerics;
using Reaper.CommonLib.Interfaces;
using Reaper.CommonLib.Utils;
using Reaper.Exchanges.Kucoin.Interfaces;
using Reaper.SignalSentinel.Strategies;
using static Reaper.Exchanges.Kucoin.Services.BackTestService;

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

    public record Pivot(
        int Index,
        PivotType Type,
        decimal Price,
        long Time); 
    
    public record BreakoutData(int Index, decimal Price, SignalType Signal, long Time);


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
                    lows.Select(x => (double)x.Price).ToList(),
                    lows.Select(x => (double)x.Index).ToList());
            var (hSlope, hIntercept, hRSquared) = CalculateRegression(
                highs.Select(x => (double)x.Price).ToList(),
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



    private static BreakoutData GetBreakOutSignal(List<FuturesKline> priceData, int candle, int backcandles, int window)
    {
        var breakoutData = new BreakoutData(
            candle,
            priceData[candle].Close,
            SignalType.Undefined,
            priceData[candle].Time); 

        if (candle - backcandles - window < 0 || candle + 20 >= priceData.Count){
            return breakoutData;
        }


        var pivots = priceData.Select((c, index) => GetPivot(priceData, index, window));
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
            ? new BreakoutData(candle, currCandle.Close, SignalType.Buy, currCandle.Time)
            : breakoutToLow
                ? new BreakoutData(candle, currCandle.Close, SignalType.Sell, currCandle.Time)
                : breakoutData;
        
    }

    internal static TradeState Trade(TradeState tradeState, SignalType actionToTake, decimal currentPrice) 
        => actionToTake == SignalType.Buy
            ? BackTestService.TryBuy(actionToTake, currentPrice, tradeState)
            : actionToTake == SignalType.Sell
                ? BackTestService.TrySell(actionToTake, currentPrice, tradeState)
                : tradeState;

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

        var window = 3;
        var backcandles = 10;

        var prices = priceData.Data!.ToList();

        var pivots = prices.Select((c, index) => GetPivot(prices, index, window));
        var breakOuts = prices.Select((c, index) => GetBreakOutSignal(prices, index, backcandles, window));

        var channel = CollectChannel(pivots);

        var channelLow = (int x) => channel.SlopeLows * x + channel.InterceptLows;
        var channelHigh = (int x) => channel.SlopeHighs * x + channel.InterceptHighs;


        string file = new FileInfo(Environment.CurrentDirectory + "/Futures/analysis/breakout.csv")
                            .FullName.Replace("api", "services");
        using var writer = new StreamWriter(file);
        // Write the header line
        writer.WriteLine("Date,Open,High,Low,Close,ChannelLow,ChannelHigh,PivotType,PivotValue,breakoutAction,tradeAmount");



        TradeState tradeState = new(
            CurrentPos: SignalType.Undefined,
            Assets: 0m,
            TradeCapital: 100m,
            EntryPrice: -1);


        for (int i=0; i < prices.Count; i++)
        {
            var candle = prices[i];
            string dateString = candle.Time.FromUtcMsToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            var breakoutAction = breakOuts.ElementAt(i);

            tradeState = Trade(tradeState, breakoutAction.Signal, currentPrice: candle.Close);
            if (breakoutAction.Signal != SignalType.Undefined)
            {
                Console.WriteLine($"Trade action: {breakoutAction.Signal} at {dateString}");
                Console.WriteLine($"Trade amount: {tradeState.TradeCapital}");
            }
            // Write the data line
            writer.WriteLine($"{dateString}," +
                $"{candle.Open}," +
                $"{candle.High}," +
                $"{candle.Low}," +
                $"{candle.Close}," +
                $"{channelLow(i)}," + 
                $"{channelHigh(i)}," + 
                $"{pivots.ElementAt(i)?.Type}," + 
                $"{pivots.ElementAt(i)?.Price}," +
                $"{breakoutAction.Signal}," +
                $"{tradeState.TradeCapital}");
        }
        decimal profit = 0m;
        var lastPrice = prices.Last().Close;
        if (tradeState.CurrentPos == SignalType.Buy)
        {
            profit = tradeState.Assets * (lastPrice - tradeState.EntryPrice);
        }
        else if (tradeState.CurrentPos == SignalType.Sell)
        {
            profit = tradeState.Assets * (tradeState.EntryPrice - lastPrice); 
        }

        Console.WriteLine($"\n\n Final Trade amount: {tradeState.TradeCapital + profit}");
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

            if (breakoutSignal.Signal != SignalType.Undefined)
            {
                return new() { Data = (symbolDetail.Data.Symbol, breakoutSignal.Signal)};
            }
        }

        return new() { Data = (string.Empty, SignalType.Undefined) };
    }
}