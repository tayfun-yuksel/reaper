namespace Reaper.CommonLib.Interfaces;
public interface IBrokerService
{
    Task<bool> BuyLimitAsync(string symbol, decimal quantity, decimal price, CancellationToken cancellationToken);
    Task<string> BuyMarketAsync(string symbol, decimal amount, CancellationToken cancellationToken);
    Task<bool> SellLimitAsync(string symbol, decimal quantity, decimal price, CancellationToken cancellationToken);
    Task<string> SellMarketAsync(string symbol, decimal amount, CancellationToken cancellationToken);
}
