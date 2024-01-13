namespace Reaper.CommonLib.Interfaces;
public interface ITransferService
{
    Task<bool> TransferToSpotAsync(string asset, decimal amount, CancellationToken cancellationToken);
    Task<bool> TransferToMarginAsync(string asset, decimal amount, CancellationToken cancellationToken);
    Task<bool> TransferToFutureAsync(string asset, decimal amount, CancellationToken cancellationToken);
}