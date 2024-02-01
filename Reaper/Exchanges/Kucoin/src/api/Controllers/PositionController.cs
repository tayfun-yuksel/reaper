
using Microsoft.AspNetCore.Mvc;
using Reaper.Exchanges.Kucoin.Interfaces;

namespace Reaper.Exchanges.Kucoin.Api;
[ApiController]
[Route("[controller]")]
public class PositionController(IPositionInfoService positionService) : ControllerBase
{
    [HttpGet(nameof(GetPositionAmountAsync))]
    public async Task<IActionResult> GetPositionAmountAsync(string symbol, CancellationToken cancellationToken)
    {
        return Ok(await positionService.GetPositionInfoAsync(symbol, cancellationToken));
    }
}