
using Microsoft.AspNetCore.Mvc;
using Reaper.CommonLib.Interfaces;

namespace Reaper.Exchanges.Binance.Api;
[ApiController]
[Route("[controller]")]
public class AccountController(IBalanceService balanceService) : ControllerBase
{
    [HttpGet("{accountId}")]
    public async Task<IActionResult> GetBalance(int accountId, CancellationToken cancellationToken)
    {
        var balance = await balanceService.GetBalanceAsync<string>(cancellationToken);
        return Ok(balance);
    }
}