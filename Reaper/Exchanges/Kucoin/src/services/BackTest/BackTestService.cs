using System.Globalization;
using Reaper.SignalSentinel.Strategies;

namespace Reaper.Exchanges.Kucoin.Services;
public class BackTestService(IMarketDataService marketDataService) : IBackTestService
{
    public record TradeState (SignalType CurrentPos, decimal Assets, decimal TradeCapital, decimal EntryPrice);

    internal static void LogTradeState(TradeState state, string symbol, decimal currentPrice)
    {
        RLogger.AppLog.Information($"{state.CurrentPos.ToString().ToUpper()} {symbol.ToUpper()} AT {DateTime.UtcNow}.....");
        RLogger.AppLog.Information($"Current Price: {currentPrice}");
        RLogger.AppLog.Information($"Assets: {state.Assets}");
        RLogger.AppLog.Information($"Current Balance: {state.Assets * currentPrice} \n");
    }

    internal static decimal GetProfitAmount(
        decimal entryPrice,
        decimal currentPrice,
        decimal assests,
        SignalType position)
    {
        if (position != SignalType.Buy && position != SignalType.Sell)
        {
            return 0;
        }

        decimal profit;
        if (position == SignalType.Buy)
        {
            profit = (currentPrice - entryPrice) * assests;
            RLogger.AppLog.Information($"Realized Profit From Long: {profit}");
            return profit;
        }

        profit = (entryPrice - currentPrice) * assests;
        RLogger.AppLog.Information($"Realized Profit From Short: {profit}");
        return profit;
    }


    internal async Task<List<decimal>> GetKlinesAsync(
        string symbol,
        string startTime,
        string? endTime,
        int interval,
        CancellationToken cancellationToken)
    {
        var now = DateTime.Now;

        static (string fromStr, string endStr) shiftTime200Kline(string start, int interval)
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
        }

        List<decimal> prices = [];
        var (from, to) = shiftTime200Kline(startTime, interval);

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
            (from, to) = shiftTime200Kline(from, interval);

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


    public static TradeState TryBuy(
        SignalType actionToTake,
        decimal currentPrice,
        TradeState tradeState)
    {
        if (tradeState.CurrentPos != SignalType.Buy && actionToTake == SignalType.Buy)
        {
            var assets = tradeState.TradeCapital / currentPrice;
            var tradingCapital = tradeState.TradeCapital +  GetProfitAmount(
                        tradeState.EntryPrice,
                        currentPrice,
                        tradeState.Assets,
                        tradeState.CurrentPos);

            return new(SignalType.Buy, assets, tradingCapital, EntryPrice: currentPrice);
        }
        return tradeState;
    }

    public static TradeState TrySell(
        SignalType actionToTake,
        decimal currentPrice,
        TradeState tradeState)
    {
        if (tradeState.CurrentPos != SignalType.Sell && actionToTake == SignalType.Sell)
        {

            var assets = tradeState.TradeCapital / currentPrice;
            var tradeCapital = tradeState.TradeCapital + GetProfitAmount(
                tradeState.EntryPrice,
                currentPrice,
                assets,
                tradeState.CurrentPos);

            return new(SignalType.Sell, assets, tradeCapital, EntryPrice: currentPrice);
        }
        return tradeState;
    }
 

    public async Task<decimal> TradeWithMultipleIndicatorsAsync(
        string symbol,
        string startTime,
        string? endTime,
        int interval,
        decimal tradeAmount,
        string[] indicators,
        CancellationToken cancellationToken)
    {
        var validIndicators = new [] { "bollinger", "tilson", "macd" };
        if (indicators.Any(x => !validIndicators.Contains(x.ToLower())))
        {
            throw new InvalidOperationException("Invalid Indicator");
        }

        var prices = (await GetKlinesAsync(
            symbol,
            startTime,
            endTime,
            interval,
            cancellationToken)).ToArray();



        var tilsonValues = indicators.Contains("tilson") 
            ? Tilson.GetTilsonValues(prices)
            : [];

        var bollingerValues = indicators.Contains("bollinger")
            ? Bollinger.GetBollingerBands(prices)
            : new Bollinger.Bands([], [], []);

        var macdValues = indicators.Contains("macd")
            ? MACD.GetMACDValues(prices)
            : new MACD.MACDValues([], []);

        TradeState tradeState = new(
            CurrentPos: SignalType.Undefined,
            Assets: 0,
            TradeCapital: tradeAmount,
            EntryPrice: 0);


        var offset = indicators.Select(indicator =>
        {
            var indicatorSwitch = indicator.ToLower();
            return indicatorSwitch switch
            {
                "bollinger" => Bollinger.PERIOD,
                "tilson" => Tilson.PERIOD,
                "macd" => MACD.PERIOD,
                _ => throw new InvalidOperationException("Invalid Indicator"),
            };
        }).Max();


        for (int i = offset; i < prices.Length; i++)
        {
            decimal currentPrice = prices[i];
            var signals = indicators.Select(indicator =>
            {
                var indicatorSwitch = indicator.ToLower();
                return indicatorSwitch switch
                {
                    "bollinger" => Bollinger.BollingerSignal(i, prices, bollingerValues),
                    "tilson" => Tilson.TilsonSignal(i, prices, tilsonValues),
                    "macd" => MACD.MACDSignal(i, macdValues),
                    _ => throw new InvalidOperationException("Invalid Indicator"),
                };
            });



            if (signals.All(x => x == SignalType.Buy))
            {
                tradeState = TryBuy(SignalType.Buy,
                    currentPrice,
                    tradeState);
            }
            else if (signals.All(x => x == SignalType.Sell))
            {
                tradeState = TrySell(SignalType.Sell,
                    currentPrice,
                    tradeState);
            }

        }

        return tradeState.TradeCapital;
    }
}