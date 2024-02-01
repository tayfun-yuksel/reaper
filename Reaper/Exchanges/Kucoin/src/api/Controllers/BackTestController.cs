using Microsoft.AspNetCore.Mvc;
using Reaper.Exchanges.Kucoin.Services;

namespace Reaper.Exchanges.Binance.Api;
[ApiController]
[Route("[controller]")]
public class BackTestController(IBackTestService backTestService) : ControllerBase
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="startTime"></param>
    /// <param name="endTime"></param>
    /// <param name="interval">1,5,15,30,60,120,240,480,720,1440,10080.</param>
    /// <param name="strategy"></param>
    /// <param name="tradeAmount"></param>
    /// <param name="volumeFactor"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet(nameof(TilsonT3))]
    public async Task<IActionResult> TilsonT3(string symbol,
        string startTime,
        string? endTime,
        int interval,
        decimal tradeAmount,
        decimal volumeFactor,
        CancellationToken cancellationToken)
    {
        if (!TimeExtensions.AllowedIntervalInMinutes.Contains(interval))
        {
            throw new ArgumentException("Interval must be one of the following: " 
                + string.Join(", ", TimeExtensions.AllowedIntervalInMinutes));
        }
        var finalAmount = await backTestService.TilsonT3Async(symbol, startTime, endTime, interval, tradeAmount, volumeFactor, cancellationToken);
        Console.WriteLine($"tilson-amount: {finalAmount}");
        return Ok(finalAmount);
    }


    [HttpGet(nameof(MACD))]
    public async Task<IActionResult> MACD(string symbol,
        string startTime,
        string? endTime,
        int interval,
        decimal tradeAmount,
        int shortPeriod,
        int longPeriod,
        int smoothLine,
        CancellationToken cancellationToken)
    {
        if (!TimeExtensions.AllowedIntervalInMinutes.Contains(interval))
        {
            throw new ArgumentException("Interval must be one of the following: " 
                + string.Join(", ", TimeExtensions.AllowedIntervalInMinutes));
        }
        var finalAmount = await backTestService.MACDAsync(symbol, startTime, endTime, interval, tradeAmount,
                                                          shortPeriod, longPeriod, smoothLine, cancellationToken);

        Console.WriteLine($"macd-amount: {finalAmount}");
        return Ok(finalAmount);
    }


    [HttpGet(nameof(BolingerBands))]
    public async Task<IActionResult> BolingerBands(string symbol,
        string startTime,
        string? endTime,
        int interval,
        decimal tradeAmount,
        int period,
        decimal deviationMultiplier,
        CancellationToken cancellationToken)
    {
        if (!TimeExtensions.AllowedIntervalInMinutes.Contains(interval))
        {
            throw new ArgumentException("Interval must be one of the following: " 
                + string.Join(", ", TimeExtensions.AllowedIntervalInMinutes));
        }
        var finalAmount = await backTestService.BollingerBandsAsync(symbol, startTime, endTime, interval, tradeAmount,
                                                                   period, deviationMultiplier, cancellationToken);

        Console.WriteLine($"bolinger-amount: {finalAmount}");
        return Ok(finalAmount);
    }


    [HttpGet(nameof(BolingerBandsAndTilsonT3))]
    public async Task<IActionResult> BolingerBandsAndTilsonT3(string symbol,
        string startTime,
        string? endTime,
        int interval,
        decimal tradeAmount,
        decimal tilsonVolumeFactor,
        int tilsonPeriod,
        int bollingerPeriod,
        decimal deviationMultiplier,
        CancellationToken cancellationToken)
    {
        if (!TimeExtensions.AllowedIntervalInMinutes.Contains(interval))
        {
            throw new ArgumentException("Interval must be one of the following: " 
                + string.Join(", ", TimeExtensions.AllowedIntervalInMinutes));
        }
        var finalAmount = await backTestService.BollingerBandsAndTilsonT3Async(symbol, startTime, endTime, interval,
                                                                                tradeAmount, tilsonVolumeFactor,
                                                                                tilsonPeriod, bollingerPeriod,
                                                                                deviationMultiplier,
                                                                                cancellationToken);

        Console.WriteLine($"bolinger-tilson-amount: {finalAmount}");
        return Ok(finalAmount);
    }
}
