using Microsoft.AspNetCore.Mvc;
using Reaper.Exchanges.Kucoin.Interfaces;

namespace Reaper.Exchanges.Kucoin.Api;
[ApiController]
[Route("api/[controller]")]
public class BreakoutController(IBreakout breakout) : ControllerBase
{

   [HttpGet("PrepareDataForPlotting")]
   public async Task<IActionResult> PrepareDataForPlotting(string symbol, string startTime, int interval)
   {
       await breakout.PrepareDataForPlottingAsync(symbol, startTime, interval);
       return Ok("done");
   } 
}