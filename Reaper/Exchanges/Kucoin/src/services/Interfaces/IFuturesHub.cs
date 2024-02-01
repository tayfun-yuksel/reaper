using Reaper.SignalSentinel.Strategies;

namespace Reaper.Exchanges.Kucoin.Interfaces;
public interface IFuturesHub
{
    Task<(bool takeProfit, decimal profit)> WatchTargetProfitAsync(decimal targetPnl, TimeSpan watchTime, string symbol); 
}