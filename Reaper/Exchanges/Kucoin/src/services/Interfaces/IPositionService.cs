
namespace Reaper.Exchanges.Kucoin.Interfaces;
public interface IPositionService
{
    Task<decimal> GetPositionAmountAsync(string symbol, CancellationToken cancellationToken);
}