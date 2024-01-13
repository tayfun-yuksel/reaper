namespace Reaper.CommonLib.Interfaces;
public interface IOrderService
{
    Task<IEnumerable<TOrder>> GetOrdersBySymbolAsync<TOrder>(
        string symbol,
        DateTime from,
        DateTime to)
    where TOrder: class;
}