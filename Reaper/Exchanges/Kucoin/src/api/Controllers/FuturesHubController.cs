using Microsoft.AspNetCore.Mvc;
using Reaper.Exchanges.Kucoin.Interfaces;
using Reaper.Exchanges.Kucoin.Services;

namespace Reaper.Exchanges.Kucoin.Api;
[ApiController]
[Route("[controller]")]
public class FuturesHubController(IFuturesHub futuresHub,
    IMarketDataService marketDataService) : ControllerBase
{
    [HttpGet("WatchTargetProfit")]
    public async Task WatchTargetProfitAsync(
        [FromQuery] string symbol,
        [FromQuery] decimal profitPercentage,
        CancellationToken cancellationToken)
    {
        RLogger.AppLog.Information($"Watching for profit on {symbol} at {DateTime.UtcNow}");
        var markPriceResult = await marketDataService.GetSymbolPriceAsync(
            symbol,
            cancellationToken);

        var result = await futuresHub.WatchTargetProfitAsync(
            symbol,
            markPriceResult.Data,
            profitPercentage,
            cancellationToken);
    }


    [HttpGet("MonitorPositionChange")]
    public async Task<IActionResult> MonitorPositionChangeAsync(
        [FromQuery] string symbol,
        [FromQuery] int interval,
        CancellationToken cancellationToken)
    {
        RLogger.AppLog.Information($"Monitoring target action for {symbol} at {DateTime.UtcNow}");
        var result = await futuresHub.MonitorPositionChangeAsync(
            symbol,
            interval,
            cancellationToken);

        return Ok(result);
    }
}