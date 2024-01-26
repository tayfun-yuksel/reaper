using Microsoft.AspNetCore.Mvc;
using Reaper.Exchanges.Kucoin.Services;

namespace Reaper.Exchanges.Binance.Api;
[ApiController]
[Route("[controller]")]
public class BackTestController(IBackTestService backTestService, IMarketDataService marketDataService) : ControllerBase
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="startTime"></param>
    /// <param name="endTime"></param>
    /// <param name="interval">1,5,15,30,60,120,240,480,720,1440,10080.</param>
    /// <param name="strategy"></param>
    /// <param name="tradeAmount"></param>
    /// <param name="volumeFactor"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet(nameof(BackTest))]
    public async Task<IActionResult> BackTest(string symbol,
        string startTime,
        string? endTime,
        int interval,
        string strategy,
        decimal tradeAmount,
        decimal? volumeFactor,
        CancellationToken cancellationToken)
    {
        var klines = await marketDataService.GetKlinesAsync(symbol, startTime, endTime, interval, cancellationToken);
        var finalAmount = backTestService.BackTest(tradeAmount, klines, strategy, volumeFactor, cancellationToken);
        return Ok(finalAmount);
    }
}