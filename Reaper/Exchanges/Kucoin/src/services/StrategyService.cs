using Reaper.Exchanges.Kucoin.Interfaces;

namespace Reaper.Exchanges.Kucoin.Services;
public class StrategyService(ITilsonService tilsonService) : IStrategyService
{
    public async Task RunAsync(string strategy, string symbol, decimal amount, int interval, CancellationToken cancellationToken)
    {
        switch (strategy.ToLower())
        {
            case "tilson":
            default:
                await tilsonService.RunAsync(symbol, amount, interval, cancellationToken);
                break;
        }
    }

}