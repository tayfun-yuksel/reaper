using Reaper.SignalSentinel.Strategies;

namespace Reaper.Exchanges.Kucoin.Services;
public class Tilson
{
    public static readonly int PERIOD = 6;
    public static readonly decimal SMOOTH = 0.5m;

    public static decimal[] GetTilsonValues(decimal[] pricesList) 
        => TilsonT3.CalculateT3(pricesList, PERIOD, SMOOTH);

    public static SignalType TilsonSignal(int index, decimal[] pricesList, decimal[] tilsonValues)
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
    }
}