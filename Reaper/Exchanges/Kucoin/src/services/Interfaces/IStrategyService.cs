
namespace Reaper.Exchanges.Kucoin.Interfaces;
public interface IStrategyService
{
    Task RunAsync(string strategy, string symbol, decimal amount, int interval, CancellationToken cancellationToken);
}