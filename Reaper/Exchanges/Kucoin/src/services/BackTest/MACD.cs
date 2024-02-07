using Reaper.SignalSentinel.Strategies;

namespace Reaper.Exchanges.Kucoin.Services;
public class MACD
{
    public static readonly int PERIOD = 11;
    public static readonly int SLOWPERIOD = 26;
    public static readonly int SMOOTH = 9;

    public record MACDValues(decimal[] MACDLines, decimal[] SignalLines); 

    public static MACDValues GetMACDValues(decimal[] pricesList)
    {
        var (macdLines, signalLines) = SignalSentinel.Strategies.MACD
                        .CalculateMACD(pricesList, fastLength: PERIOD, SLOWPERIOD, SMOOTH);
        return new MACDValues(macdLines, signalLines);
    }

    
    public static SignalType MACDSignal(int index, MACDValues mACDValues)
    {
        if (mACDValues.MACDLines[index] > mACDValues.SignalLines[index])
        {
            return SignalType.Buy;
        }
        else if (mACDValues.MACDLines[index] < mACDValues.SignalLines[index])
        {
            return SignalType.Sell;
        }

        return SignalType.Hold;
    }
}