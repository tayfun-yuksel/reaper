
namespace Reaper.Exchanges.Kucoin.Services;
public interface IMarketDataService
{
    Task<IEnumerable<decimal>> GetKlinesAsync(string symbol, string startTime, string? endTime, int interval, CancellationToken cancellationToken);
    Task<decimal> GetSymbolPriceAsync(string symbol, CancellationToken cancellationToken);
}