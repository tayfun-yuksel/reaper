
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
        [FromQuery] decimal profitPercentage,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}