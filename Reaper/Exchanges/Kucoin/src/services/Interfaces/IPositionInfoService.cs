
using Reaper.CommonLib.Interfaces;
using Reaper.SignalSentinel.Strategies;

namespace Reaper.Exchanges.Kucoin.Interfaces;
public interface IPositionInfoService
{
    Task<Result<(decimal amount, decimal positionEnterPrice, SignalType position)>> GetPositionInfoAsync(string symbol, CancellationToken cancellationToken);
}