using Reaper.SignalSentinel.Strategies;

namespace Reaper.Exchanges.Kucoin.Services;
public interface IBackTestService
{
    decimal BackTest(decimal tradeAmount, IEnumerable<decimal> prices, string strategy, CancellationToken cancellationToken);
}