using Microsoft.AspNetCore.Mvc;
using Reaper.Exchanges.Kucoin.Services;

namespace Reaper.Exchanges.Binance.Api;
[ApiController]
[Route("[controller]")]
public class BackTestController(IBackTestService backTestService) : ControllerBase
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
        if (!TimeExtensions.AllowedIntervalInMinutes.Contains(interval))
        {
            throw new ArgumentException("Interval must be one of the following: " 
                + string.Join(", ", TimeExtensions.AllowedIntervalInMinutes));
        }
        var finalAmount = await backTestService.BackTestAsync(symbol, startTime, endTime, interval, tradeAmount, strategy, volumeFactor, cancellationToken);
        return Ok(finalAmount);
    }
}