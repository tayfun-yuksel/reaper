
namespace Reaper.Exchanges.Kucoin.Interfaces;
public interface ITilsonService
{
    Task RunAsync(
        string symbol,
        decimal amount,
        decimal profitPercentage,
        int interval,
        CancellationToken cancellationToken);
}