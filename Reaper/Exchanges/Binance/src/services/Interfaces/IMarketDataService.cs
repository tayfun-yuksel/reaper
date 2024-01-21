
using Reaper.Exchanges.Binance.Services.ApiModels;

namespace Reaper.Exchanges.Binance.Interfaces;
public interface IMarketDataService
{
    Task<decimal> GetSymbolPriceAsync(string symbol, CancellationToken cancellationToken);
    Task<SymbolExchangeInfoResponse> GetSymbolExchangeInfoAsync(string symbol, CancellationToken cancellationToken);
    Task<string> GetKlinesAsync(string symbol, string startTime, string? endTime, CancellationToken cancellationToken);
}