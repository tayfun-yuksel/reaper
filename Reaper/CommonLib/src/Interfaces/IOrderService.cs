namespace Reaper.CommonLib.Interfaces;
public interface IOrderService
{
    Task<Result<IEnumerable<TOrder>>> GetOrdersBySymbolAsync<TOrder>(
        string symbol,
        DateTime from,
        DateTime to)
    where TOrder: class;

    Task<Result<string>> GetOrdersAsync(IDictionary<string, object?> parameters, CancellationToken cancellationToken);
}