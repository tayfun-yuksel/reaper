
using Reaper.Exchanges.Binance.Services.ApiModels;

namespace Reaper.Exchanges.Binance.Services;
public interface IMarketDataService
{
    Task<decimal> GetSymbolPriceAsync(string symbol, CancellationToken cancellationToken);
    Task<SymbolExchangeInfoResponse> GetSymbolExchangeInfoAsync(string symbol, CancellationToken cancellationToken);
    Task<IEnumerable<decimal>> GetKlinesAsync(string symbol, string startTime, string? endTime, string interval, CancellationToken cancellationToken);
}