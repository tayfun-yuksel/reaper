namespace Reaper.CommonLib.Interfaces;
public interface IOrderService
{
    Task<IEnumerable<TOrder>> GetOrdersBySymbolAsync<TOrder>(
        string symbol,
        DateTime from,
        DateTime to)
    where TOrder: class;

    Task<Result<string>> GetOrdersAsync(IDictionary<string, object?> parameters, CancellationToken cancellationToken);
}