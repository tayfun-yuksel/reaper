
namespace Reaper.SignalSentinel.Strategies.test;
public class CalculateBollngerBands
{

    [Fact]
    public void Test1()
    {
        decimal[] prices = [1m, 2m, 3m, 4m, 5m, 6m, 7m, 8m];
        int period = 5;
        decimal standardDeviationMultiplier = 0.5m;

        var (upperBand, middleBand, lowerBand) = BollingerBands.CalculateBollingerBands(prices, period, standardDeviationMultiplier);

        Assert.Equal(upperBand[7], 7.5m);
        Assert.Equal(lowerBand[7], 2.5m);
    }
}