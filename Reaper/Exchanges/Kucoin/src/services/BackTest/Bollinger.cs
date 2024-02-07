using Reaper.SignalSentinel.Strategies;

namespace Reaper.Exchanges.Kucoin.Services;
public class Bollinger
{
    public static readonly int PERIOD = 6;
    public record Bands(decimal[] UpperBand, decimal[] MiddleBand, decimal[] LowerBand);

    public static Bands GetBollingerBands(decimal[] pricesList)
    {
        var deviationMultiplier = 17m;

        var (upperBand, middleBand, lowerBand) = BollingerBands.CalculateBollingerBands(
            pricesList,
            PERIOD,
            deviationMultiplier);

        return new(upperBand, middleBand, lowerBand);
    }

    public static SignalType BollingerSignal(
        int index,
        decimal[] pricesList,
        Bands bands)
    {
        var currentPrice = pricesList[index];
        var deltaUpper = bands.UpperBand[index] - currentPrice;
        var deltaLower = currentPrice - bands.LowerBand[index];


        if (deltaUpper > deltaLower)
        {
            return SignalType.Buy;
        }
        else if (deltaUpper < deltaLower)
        {
            return SignalType.Sell;
        }
        return SignalType.Hold;
    }
}