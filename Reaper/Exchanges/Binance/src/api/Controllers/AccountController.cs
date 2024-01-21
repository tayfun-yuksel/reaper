
using Microsoft.AspNetCore.Mvc;
using Reaper.CommonLib.Interfaces;
using Reaper.Exchanges.Binance.Services.ApiModels;

namespace Reaper.Exchanges.Binance.Api;
[ApiController]
[Route("[controller]")]
public class AccountController(IBalanceService balanceService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetBalance([FromQuery] string symbol, CancellationToken cancellationToken)
    {
        var balance = await balanceService.GetBalanceAsync<Balance>(symbol, cancellationToken);
        return Ok(balance);
    }
}