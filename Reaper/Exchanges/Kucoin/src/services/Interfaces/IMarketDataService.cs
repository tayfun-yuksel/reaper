
using Reaper.CommonLib.Interfaces;

namespace Reaper.Exchanges.Kucoin.Services;
public interface IMarketDataService
{
    Task<Result<IEnumerable<decimal>>> GetKlinesAsync(string symbol, string startTime, string? endTime, int interval, CancellationToken cancellationToken);
    Task<Result<decimal>> GetSymbolPriceAsync(string symbol, CancellationToken cancellationToken);
}