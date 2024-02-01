
using Microsoft.AspNetCore.Mvc;
using Reaper.Exchanges.Kucoin.Interfaces;

namespace Reaper.Exchanges.Kucoin.Api;
[ApiController]
[Route("[controller]")]
public class FuturesHubController(IFuturesHub futuresHub) : ControllerBase
{
    [HttpGet("WatchAndSignalProfit")]
    public async Task WathcAndSignalProfitAsync(
        [FromQuery] int watchTime,
        [FromQuery] string symbol,
        [FromQuery] decimal tradeAmount,
        [FromQuery] decimal profitPercentage,
        CancellationToken cancellationToken)
    {
        var limit = TimeSpan.FromMinutes(watchTime);
        var targetPnl = tradeAmount * profitPercentage;
        await futuresHub.WatchTargetProfitAsync(targetPnl, limit, symbol);
    }
}