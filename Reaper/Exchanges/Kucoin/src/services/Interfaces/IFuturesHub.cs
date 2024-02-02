using Reaper.SignalSentinel.Strategies;

namespace Reaper.Exchanges.Kucoin.Interfaces;
public interface IFuturesHub
{
    Func<decimal, TimeSpan, string, Task<(bool takeProfit, decimal profit)>> WatchTargetProfitAsync { get; }
}