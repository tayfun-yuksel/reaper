
using Microsoft.AspNetCore.Mvc;
using Reaper.CommonLib.Interfaces;

namespace Reaper.Exchanges.Binance.Api;
[ApiController]
[Route("[controller]")]
public class BrokerController(IBrokerService brokerService) : ControllerBase
{
    private readonly IBrokerService brokerService = brokerService;

    [HttpGet(nameof(BuyLimit))]
    public async Task<IActionResult> BuyLimit(string symbol, decimal quantity, decimal price, CancellationToken cancellationToken)
    {
        var exchangeInfo = await brokerService.BuyLimitAsync(symbol, quantity, price, cancellationToken);
        return Ok(exchangeInfo);
    }

    [HttpGet(nameof(BuyMarket))]
    public async Task<IActionResult> BuyMarket(string symbol, decimal quantity, CancellationToken cancellationToken)
    {
        var exchangeInfo = await brokerService.BuyMarketAsync(symbol, quantity, cancellationToken);
        return Ok(exchangeInfo);
    }


    [HttpGet(nameof(SellMarket))]
    public async Task<IActionResult> SellMarket(string symbol, decimal quantity, CancellationToken cancellationToken)
    {
        var exchangeInfo = await brokerService.SellMarketAsync(symbol, quantity, cancellationToken);
        return Ok(exchangeInfo);
    }

}