using Microsoft.AspNetCore.Mvc;
namespace Reaper.Kucoin.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class BrokerController : ControllerBase
{
    public BrokerController()
    {
        
    }

    [HttpGet(nameof(BuyLimitAsync))]
    public async Task BuyLimitAsync(string symbol, decimal quantity, decimal price, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
    


    [HttpGet(nameof(BuyMarketAsync))]
    public async Task BuyMarketAsync(string symbol, decimal quantity, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }


    [HttpGet(nameof(SellLimitAsync))]
    public async Task SellLimitAsync(string symbol, decimal quantity, decimal price, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }


    [HttpGet(nameof(SellMarketAsync))]
    public async Task SellMarketAsync(string symbol, decimal quantity, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}