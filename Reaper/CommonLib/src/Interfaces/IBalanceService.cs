namespace Reaper.CommonLib.Interfaces;
public interface IBalanceService
{
    Task<TBalance?> GetBalanceAsync<TBalance>(string? symbol, CancellationToken cancellationToken)
    where TBalance: class;
}