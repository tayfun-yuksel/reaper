using Reaper.CommonLib.Interfaces;

namespace Reaper.Exchanges.Services.Kucoin;
public class BrokerService : IBrokerService
{
    public Task<bool> BuyLimitAsync(string symbol, decimal quantity, decimal price, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<bool> BuyMarketAsync(string symbol, decimal quantity, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<bool> SellLimitAsync(string symbol, decimal quantity, decimal price, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<bool> SellMarketAsync(string symbol, decimal quantity, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
