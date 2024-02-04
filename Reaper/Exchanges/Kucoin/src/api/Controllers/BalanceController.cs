using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Reaper.CommonLib.Interfaces;
using Reaper.Exchanges.Kucoin.Services.Models;

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