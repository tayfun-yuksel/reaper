using Reaper.CommonLib.Interfaces;

namespace Reaper.Exchanges.Kucoin.Services;
public interface IMarketDataService
{
    Task<Result<IEnumerable<SymbolDetail>>> GetSymbolsAsync(CancellationToken cancellationToken);
    Task<Result<decimal>> GetSymbolPriceAsync(string symbol, CancellationToken cancellationToken);
    Task<Result<IEnumerable<FuturesKline>>> GetKlinesAsync(string symbol, string startTime, string? endTime, int interval, CancellationToken cancellationToken);
}