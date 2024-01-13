namespace Reaper.CommonLib.Interfaces;
public interface IBalanceService
{
    Task<TBalance> GetBalanceAsync<TBalance>(CancellationToken cancellationToken)
    where TBalance: class;
}