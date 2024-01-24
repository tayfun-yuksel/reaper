using Microsoft.AspNetCore.Mvc;
using Reaper.Exchanges.Kucoin.Services;

namespace Reaper.Exchanges.Binance.Api;
[ApiController]
[Route("[controller]")]
public class BackTestController(IBackTestService backTestService, IMarketDataService marketDataService) : ControllerBase
{
    private readonly IBackTestService _backTestService = backTestService;
    private readonly IMarketDataService _marketDataService = marketDataService;

    [HttpGet(nameof(BackTest))]
    public async Task<IActionResult> BackTest(string symbol,
        string startTime,
        string? endTime,
        string interval,
        string strategy,
        decimal tradeAmount,
        CancellationToken cancellationToken)
    {
        var klines = await _marketDataService.GetFutureKlinesAsync(symbol, startTime, endTime, interval, cancellationToken);
        var finalAmount = _backTestService.BackTest(tradeAmount, klines, strategy, cancellationToken);
        return Ok(finalAmount);
    }
}