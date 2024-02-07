using Microsoft.AspNetCore.Mvc;
using Reaper.Exchanges.Kucoin.Services;

namespace Reaper.Exchanges.Binance.Api;
[ApiController]
[Route("[controller]")]
public class BackTestController(IBackTestService backTestService) : ControllerBase
{

    [HttpGet("MultiStrategy")]
    public async Task<IActionResult> TestMultiStrategy(string symbol,
        string startTime,
        string? endTime,
        int interval,
        decimal tradeAmount,
        [FromQuery] string[] indicators,
        CancellationToken cancellationToken)
    {
        if (!TimeExtensions.AllowedIntervalInMinutes.Contains(interval))
        {
            throw new ArgumentException("Interval must be one of the following: " 
                + string.Join(", ", TimeExtensions.AllowedIntervalInMinutes));
        }
        var finalAmount = await backTestService.TradeWithMultipleIndicatorsAsync(
            symbol,
            startTime,
            endTime,
            interval,
            tradeAmount,
            indicators,
            cancellationToken);

        RLogger.AppLog.Information($"multi-strategy-amount: {finalAmount}");
        return Ok(finalAmount);
    }
}
