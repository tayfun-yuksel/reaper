namespace Reaper.CommonLib.Interfaces;
public interface ITransferService
{
    Task<Result<bool>> TransferToSpotAsync(string asset, decimal amount, CancellationToken cancellationToken);
    Task<Result<bool>> TransferToMarginAsync(string asset, decimal amount, CancellationToken cancellationToken);
    Task<Result<bool>> TransferToFutureAsync(string asset, decimal amount, CancellationToken cancellationToken);
}