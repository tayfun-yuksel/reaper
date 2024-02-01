
using FizzWare.NBuilder;
using Reaper.SignalSentinel.Strategies;

namespace Reaper.SignalSentinel.test;
public class CalculateMACD
{
    [Fact]
    public void ShouldCalculateMACD()
    {
        decimal[] prices =
        [
            06641m, 0.664m, 0.6519m, 0.6588m, 0.6559m, 0.6449m, 0.6295m, 0.617m, 0.6293m, 0.6332m, 0.6389m, 0.633m,
            0.6407m, 0.6522m, 0.6458m, 0.6488m, 0.649m, 0.6525m, 0.6463m, 0.6489m, 0.6428m, 0.6515m, 0.6536m, 0.6592m,
            0.6634m, 0.6627m, 0.6721m, 0.6777m, 0.6762m, 0.6798m, 0.6822m, 0.6818m, 0.6846m, 0.683m, 0.6796m, 0.6823m,
            0.6914m, 0.6982m, 0.7061m, 0.7105m, 0.7148m, 0.7043m, 0.7194m, 0.7128m, 0.7236m, 0.7324m, 0.7287m, 0.7252m,
            0.726m, 0.7156m, 0.7143m, 0.7108m, 0.707m, 0.7143m, 0.7111m, 0.7089m, 0.708m, 0.7086m, 0.7163m, 0.7101m,
            0.7152m, 0.7116m, 0.712m, 0.7148m, 0.7138m, 0.7153m, 0.7035m, 0.6984m, 0.6915m, 0.693m, 0.6856m, 0.6994m,
            0.6938m, 0.6918m, 0.6734m, 0.6758m, 0.6759m, 0.6718m, 0.6631m, 0.6692m, 0.668m, 0.6633m, 0.6682m, 0.6706m,
            0.6666m, 0.6689m, 0.6663m, 0.6567m, 0.6594m, 0.6533m, 0.6473m, 0.6555m, 0.6527m, 0.6716m, 0.6701m, 0.6742m,
            0.675m, 0.6847m, 0.6849m, 0.6868m, 0.6818m, 0.6853m, 0.6804m, 0.6858m, 0.6867m, 0.6892m, 0.6843m, 0.6865m,
            0.6887m, 0.6864m, 0.6827m, 0.6776m, 0.6768m, 0.6724m, 0.6741m, 0.6884m, 0.6908m, 0.6945m, 0.6892m, 0.6869m,
            0.6896m, 0.6777m, 0.6683m, 0.6767m, 0.6717m, 0.676m, 0.6613m, 0.668m, 0.6747m, 0.6773m
        ];
        int fastLength = 12;
        int slowLength = 26;
        int signalSmoothing = 9;

        var (macdLine, signalLine) = MACD.CalculateMACD(prices, fastLength, slowLength, signalSmoothing);

        
    }
}