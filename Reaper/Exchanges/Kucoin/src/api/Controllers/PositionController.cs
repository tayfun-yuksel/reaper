
using Microsoft.AspNetCore.Mvc;
using Reaper.Exchanges.Kucoin.Services;

namespace Reaper.Exchanges.Kucoin.Api;
[ApiController]
[Route("[controller]")]
public class PositionController(IPositionService positionService) : ControllerBase
{
    [HttpGet(nameof(GetPositionDetailsAsync))]
    public async Task<string> GetPositionDetailsAsync(string symbol, CancellationToken cancellationToken)
    {
        return await positionService.GetPositionDetailsAsync(symbol, cancellationToken);
    }
}