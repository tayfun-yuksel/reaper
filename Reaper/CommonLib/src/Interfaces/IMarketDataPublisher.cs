namespace Reaper.CommonLib.Interfaces;
public interface IMarketDataPublisher
{
    Task GetAvaragePriceAsync(string asset, CancellationToken cancellationToken);
}