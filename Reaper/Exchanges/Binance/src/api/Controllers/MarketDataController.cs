
using Microsoft.AspNetCore.Mvc;
using Reaper.Exchanges.Binance.Interfaces;

namespace Reaper.Exchanges.Binance.Api;
[ApiController]
[Route("[controller]")]
public class MarketDataController(IMarketDataService marketDataService) : ControllerBase
{
    private readonly IMarketDataService _marketDataService = marketDataService;

    
    [HttpGet(nameof(GetSymbolPrice))]
    public async Task<IActionResult> GetSymbolPrice(string symbol, CancellationToken cancellationToken)
    {
        var symbolPrice = await _marketDataService.GetSymbolPriceAsync(symbol, cancellationToken);
        return symbolPrice > 0 ? Ok(symbolPrice) : BadRequest();
    }

    [HttpGet(nameof(GetSymbolExchangeInfo))]
    public async Task<IActionResult> GetSymbolExchangeInfo(string symbol, CancellationToken cancellationToken)
    {
        var exchangeInfo = await _marketDataService.GetSymbolExchangeInfoAsync(symbol, cancellationToken);
        return Ok(exchangeInfo);
    }


    [HttpGet(nameof(GetKlines))]
    public async Task<IActionResult> GetKlines(string symbol, string startTime, string? endTime, CancellationToken cancellationToken)
    {
        var klines = await _marketDataService.GetKlinesAsync(symbol, startTime, endTime, cancellationToken);
        return Ok(klines);
    }
}