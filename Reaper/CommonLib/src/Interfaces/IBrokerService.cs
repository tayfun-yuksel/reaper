namespace Reaper.CommonLib.Interfaces;
public interface IBrokerService
{
    Task<Result<string>> BuyLimitAsync(
        string symbol,
        decimal amount,
        int leverage,
        decimal limitPrice,
        CancellationToken cancellationToken);
    Task<Result<string>> BuyMarketAsync(
        string symbol,
        decimal amount,
        int leverage,
        CancellationToken cancellationToken);
    Task<Result<string>> SellLimitAsync(
        string symbol,
        decimal amount,
        int leverage,
        decimal limitPrice,
        CancellationToken cancellationToken);
    Task<Result<string>> SellMarketAsync(
        string symbol,
        decimal amount,
        int leverage,
        CancellationToken cancellationToken);
}
