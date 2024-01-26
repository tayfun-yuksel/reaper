using Microsoft.AspNetCore.Mvc;
using Reaper.CommonLib.Interfaces;

namespace Reaper.Exchanges.Kucoin.Api;
[ApiController]
[Route("[controller]")]
public class BalanceController(IBalanceService balanceService) : ControllerBase
{
    [HttpGet(nameof(GetBalanceAsync))]
    public async Task<IActionResult> GetBalanceAsync(string? symbol, CancellationToken cancellationToken)
    {
        var response =  await balanceService.GetBalanceAsync<string>(symbol, cancellationToken);
        return Ok(response);
    }
}