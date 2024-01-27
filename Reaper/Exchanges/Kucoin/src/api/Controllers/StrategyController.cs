using Microsoft.AspNetCore.Mvc;
using Reaper.Exchanges.Kucoin.Interfaces;

namespace Reaper.Exchanges.Kucoin.Api;
[ApiController]
[Route("[controller]")]
public class StrategyController(ITilsonService tilsonService) : ControllerBase
{
    [HttpGet(nameof(TilsonT3Async))]
    public async Task TilsonT3Async(string symbol, decimal amount, int interval, CancellationToken cancellationToken)
    {
        await tilsonService.RunAsync(symbol, amount, interval, cancellationToken);
    }
}