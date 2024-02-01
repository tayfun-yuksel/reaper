
using Reaper.SignalSentinel.Strategies;

namespace Reaper.Exchanges.Kucoin.Interfaces;
public interface IPositionInfoService
{
    Task<(decimal amount, SignalType position)> GetPositionInfoAsync(string symbol, CancellationToken cancellationToken);
}