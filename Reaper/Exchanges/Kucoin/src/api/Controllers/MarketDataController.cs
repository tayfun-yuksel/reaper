using Microsoft.AspNetCore.Mvc;
using Reaper.Exchanges.Kucoin.Services;

namespace Reaper.Exchanges.Kucoin.Api;
[ApiController]
[Route("[controller]")]
public class MarketDataController(IMarketDataService marketDataService) : ControllerBase
{

    [HttpGet(nameof(GetFutureKlines))]
    public async Task<IActionResult> GetFutureKlines(string symbol,
        string startTime,
        string? endTime,
        int interval, CancellationToken cancellationToken)
    {
        var klines = await marketDataService.GetKlinesAsync(symbol, startTime, endTime, interval, cancellationToken);
        return Ok(klines);
    }
}