
namespace Reaper.Exchanges.Kucoin.Services;
public interface IMarketDataService
{
    Task<IEnumerable<decimal>> GetFutureKlinesAsync(string symbol, string startTime, string? endTime, string interval, CancellationToken cancellationToken);
}