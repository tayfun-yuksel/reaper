using Microsoft.AspNetCore.Mvc;
using Reaper.CommonLib.Interfaces;

namespace Reaper.Exchanges.Kucoin.Api;
[ApiController]
[Route("[controller]")]
public class OrderController(IOrderService orderService) : ControllerBase
{
    
    [HttpGet(nameof(GetOrdersAsync))]
    public async Task<IActionResult> GetOrdersAsync(string? symbol,
        string? status,
        string? startAt,
        string? endAt,
        CancellationToken cancellationToken)
    {
        Dictionary<string, object?> parameters = [];
        parameters.TryAdd("symbol", symbol?.ToUpper());
        parameters.TryAdd("status", status);
        parameters.TryAdd("startAt", string.IsNullOrEmpty(startAt) 
            ? null 
            : Services.TimeExtensions.ToUtcEpochMs(startAt));
        parameters.TryAdd("endAt", string.IsNullOrEmpty(endAt) 
            ? null 
            : Services.TimeExtensions.ToUtcEpochMs(endAt));
        return Ok(await orderService.GetOrdersAsync(parameters, cancellationToken));
    }
}