using Microsoft.AspNetCore.Mvc;
using Reaper.CommonLib.Interfaces;
using Reaper.Exchanges.Kucoin.Services;
namespace Reaper.Exchanges.Kucoin.Api;

[ApiController]
[Route("[controller]")]
public class BrokerController(IBrokerService brokerService,
    IMarketDataService marketDataService) : ControllerBase
{

    [HttpGet(nameof(BuyLimitAsync))]
    public async Task<IActionResult> BuyLimitAsync(string symbol, decimal amount, CancellationToken cancellationToken)
    {
        //for testing
        var currentPrice = await marketDataService.GetSymbolPriceAsync(symbol, cancellationToken);
        if (currentPrice.Error != null)
        {
            throw new InvalidOperationException("Error getting current price", currentPrice.Error);
        }

        RLogger.AppLog.Information($"Buying {symbol} at {currentPrice.Data!} at {DateTime.UtcNow}");

        var response = await brokerService.BuyLimitAsync(
            symbol,
            amount,
            currentPrice.Data!,
            cancellationToken); 

        return Ok(response);
    }
    


    [HttpGet(nameof(BuyMarketAsync))]
    public async Task<IActionResult> BuyMarketAsync(string symbol, decimal amount, CancellationToken cancellationToken)
    {
        var response = await brokerService.BuyMarketAsync(symbol, amount, cancellationToken);
        return Ok(response);
    }


    [HttpGet(nameof(SellLimitAsync))]
    public async Task<IActionResult> SellLimitAsync(string symbol, decimal amount, CancellationToken cancellationToken)
    {
        var currentPrice = await marketDataService.GetSymbolPriceAsync(symbol, cancellationToken);
        if (currentPrice.Error != null)
        {
            throw new InvalidOperationException("Error getting current price", currentPrice.Error);
        }

        RLogger.AppLog.Information($"Selling {symbol} at {currentPrice.Data!} at {DateTime.UtcNow}");

        var response = await brokerService.SellLimitAsync(
            symbol,
            amount,
            currentPrice.Data!,
            cancellationToken);
        return Ok(response);
    }


    [HttpGet(nameof(SellMarketAsync))]
    public async Task<IActionResult> SellMarketAsync(string symbol, decimal quantity, CancellationToken cancellationToken)
    {
        var response = await brokerService.SellMarketAsync(symbol, quantity, cancellationToken);
        return Ok(response);
    }
}