namespace Reaper.Exchanges.Kucoin.Services;
public interface IBackTestService
{
    Task<decimal> BackTestAsync(string symbol, string startTime, string? endTime, int interval, decimal tradeAmount, string strategy, decimal? volumeFactor, CancellationToken cancellationToken);
}