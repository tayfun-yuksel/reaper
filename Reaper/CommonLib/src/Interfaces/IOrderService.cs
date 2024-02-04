namespace Reaper.CommonLib.Interfaces;
public interface IOrderService
{
    Task<Result<decimal>> GetOrderAmountAsync(string orderid, CancellationToken cancellationToken);

    Task<Result<IEnumerable<string>>> GetActiveOrdersAsync(string symbol, CancellationToken cancellationToken);
}