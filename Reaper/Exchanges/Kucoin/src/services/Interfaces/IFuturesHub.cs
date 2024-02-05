using Reaper.CommonLib.Interfaces;

namespace Reaper.Exchanges.Kucoin.Interfaces;
public interface IFuturesHub
{
    Task<Result<(bool takeProfit, decimal profitPercent)>> WatchTargetProfitAsync(
        string symbol,
        decimal entryPrice,
        decimal targetPnlPercent,
        CancellationToken cancellationToken);
        
    Task<Result<(bool takeProfit, decimal profitPnl)>> MonitorPositionChangeAsync(
        string symbol,
        decimal targetPnl,
        CancellationToken cancellationToken);
}