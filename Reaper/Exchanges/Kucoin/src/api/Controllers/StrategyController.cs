using Microsoft.AspNetCore.Mvc;
using Reaper.Exchanges.Kucoin.Interfaces;

namespace Reaper.Exchanges.Kucoin.Api;
[ApiController]
[Route("[controller]")]
public class StrategyController(ITilsonService tilsonService) : ControllerBase
{
    [HttpGet("Tilson")]
    public async Task TilsonT3Async(
        string symbol,
        decimal amount,
        decimal profitPercentage,
        int interval,
        CancellationToken cancellationToken)
    {
        await tilsonService.RunAsync(
            symbol,
            amount,
            profitPercentage,
            interval,
            cancellationToken);
    }
}