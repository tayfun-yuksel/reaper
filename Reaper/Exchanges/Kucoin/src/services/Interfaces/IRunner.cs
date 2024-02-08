namespace Reaper.Exchanges.Kucoin.Interfaces;
public interface IRunner
{
    Task RunAsync(
        string symbol,
        decimal amount,
        int leverage,
        decimal profitPercentage,
        int interval,
        string[] indicators,
        CancellationToken cancellationToken);
}