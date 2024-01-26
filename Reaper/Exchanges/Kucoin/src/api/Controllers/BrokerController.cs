using Microsoft.AspNetCore.Mvc;
using Reaper.CommonLib.Interfaces;
namespace Reaper.Exchanges.Kucoin.Api;

[ApiController]
[Route("[controller]")]
public class BrokerController(IBrokerService brokerService) : ControllerBase
{

    [HttpGet(nameof(BuyLimitAsync))]
    public Task BuyLimitAsync(string symbol, decimal amount, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
    


    [HttpGet(nameof(BuyMarketAsync))]
    public async Task<IActionResult> BuyMarketAsync(string symbol, decimal amount, CancellationToken cancellationToken)
    {
        var response = await brokerService.BuyMarketAsync(symbol, amount, cancellationToken);
        return Ok(response);
    }


    [HttpGet(nameof(SellLimitAsync))]
    public Task SellLimitAsync(string symbol, decimal amount, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }


    [HttpGet(nameof(SellMarketAsync))]
    public async Task<IActionResult> SellMarketAsync(string symbol, decimal quantity, CancellationToken cancellationToken)
    {
        var response = await brokerService.SellMarketAsync(symbol, quantity, cancellationToken);
        return Ok(response);
    }
}