using Reaper.CommonLib.Interfaces;

namespace Reaper.Exchanges.Kucoin.Services;
public class DiscoveryService(IMarketDataService marketDataService)
{
    public async Task<Result<IEnumerable<string>>> GetTopGainersAsync(
        string symbol,
        int limit,
        CancellationToken cancellationToken)
    {
        Result<IEnumerable<SymbolDetail>> symbols = 
                await marketDataService.GetSymbolsAsync(cancellationToken);

        if (symbols.Error != null){
            return new() { Error = symbols.Error };
        }

        var topGainers = symbols.Data!
            .Where(x => x.Symbol.Contains(symbol.ToUpper(), StringComparison.InvariantCultureIgnoreCase))
            .OrderByDescending(x => x.PriceChgPct)
            .Take(limit)
            .Select(x => x.Symbol);

        return  new() { Data = topGainers };
    }
}