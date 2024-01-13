using Microsoft.AspNetCore.Mvc;
namespace Reaper.Kucoin.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class BrokerController : ControllerBase
{
    public BrokerController()
    {
        
    }

    public async Task BuyLimitAsync(string symbol, decimal quantity, decimal price, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task BuyMarketAsync(string symbol, decimal quantity, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }


    public async Task SellLimitAsync(string symbol, decimal quantity, decimal price, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task SellMarketAsync(string symbol, decimal quantity, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}