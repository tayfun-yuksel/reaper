using Microsoft.AspNetCore.Mvc;
using Reaper.Exchanges.Kucoin.Interfaces;

namespace Reaper.Exchanges.Kucoin.Api;
[ApiController]
[Route("[controller]")]
public class StrategyController(IRunner tilsonService) : ControllerBase
{
    [HttpGet("Tilson")]
    public async Task TilsonT3Async(
        string symbol,
        decimal amount,
        int leverage,
        decimal profitPercentage,
        int interval,
        [FromQuery] string[] indicator,
        CancellationToken cancellationToken)
    {
        await tilsonService.RunAsync(
            symbol,
            amount,
            leverage,
            profitPercentage,
            interval,
            indicator,
            cancellationToken);
    }
}