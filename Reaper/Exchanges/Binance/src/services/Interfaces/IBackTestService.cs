using Reaper.SignalSentinel.Strategies;

namespace Reaper.Exchanges.Binance.Services;
public interface IBackTestService
{
    SignalType BackTest(decimal tradeAmount, IEnumerable<decimal> prices, string strategy, CancellationToken cancellationToken);
}