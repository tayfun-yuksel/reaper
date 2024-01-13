namespace Reaper.CommonLib.Interfaces;
public interface IBrokerService
{
    Task<bool> BuyLimitAsync(string symbol, decimal quantity, decimal price, CancellationToken cancellationToken);
    Task<bool> BuyMarketAsync(string symbol, decimal quantity, CancellationToken cancellationToken);
    Task<bool> SellLimitAsync(string symbol, decimal quantity, decimal price, CancellationToken cancellationToken);
    Task<bool> SellMarketAsync(string symbol, decimal quantity, CancellationToken cancellationToken);
}
