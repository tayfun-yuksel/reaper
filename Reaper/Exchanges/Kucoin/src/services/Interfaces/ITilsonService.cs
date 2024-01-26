
namespace Reaper.Exchanges.Kucoin.Interfaces;
public interface ITilsonService
{
    Task RunAsync(string symbol, decimal amount, int interval, CancellationToken cancellationToken);
}