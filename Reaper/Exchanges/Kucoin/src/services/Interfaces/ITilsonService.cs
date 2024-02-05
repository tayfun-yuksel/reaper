
namespace Reaper.Exchanges.Kucoin.Interfaces;
public interface ITilsonService
{
    Task RunAsync(
        string symbol,
        decimal amount,
        int leverage,
        decimal profitPercentage,
        int interval,
        CancellationToken cancellationToken);
}