using Microsoft.AspNetCore.Mvc;
using Reaper.Exchanges.Kucoin.Services;

namespace Reaper.Exchanges.Kucoin.Api;
[ApiController]
[Route("[controller]")]
public class MarketDataController(IMarketDataService marketDataService) : ControllerBase
{
    private readonly IMarketDataService _marketDataService = marketDataService;


    [HttpGet(nameof(GetFutureKlines))]
    public async Task<IActionResult> GetFutureKlines(string symbol,
        string startTime,
        string? endTime,
        string interval, CancellationToken cancellationToken)
    {
        var klines = await _marketDataService.GetFutureKlinesAsync(symbol, startTime, endTime, interval, cancellationToken);
        return Ok(klines);
    }
}