using Flurl.Http;
using Microsoft.Extensions.Configuration;
using Reaper.CommonLib.Interfaces;

namespace Reaper.Exchanges.Binance.Services;
public class BalanceService(IConfiguration configuration) : IBalanceService
{
    private readonly string _logLevel = "error";

    public async Task<TBalance> GetBalanceAsync<TBalance>(CancellationToken cancellationToken)
        where TBalance : class
    {
        string apiKey = configuration["Binance:ApiKey"] ?? throw new InvalidOperationException(nameof(apiKey));
        string secretKey = configuration["Binance:SecretKey"] ?? throw new InvalidOperationException(nameof(secretKey));

        using var flurlClient = FlurlExtensions.GetFlurlClient(configuration);

        var data = new{};
        var balanceFn = async (IFlurlClient client, object requestData, CancellationToken cancellation) => 
            await client.Request()
                .WithSignature(secretKey, requestData)
                .WithHeader("X-MBX-APIKEY", apiKey)
                .GetAsync(HttpCompletionOption.ResponseContentRead, cancellation)
                .WithLogging(_logLevel)
                .TryReceiveJson<string>();
        var (response, error) = await balanceFn.CallAsync(flurlClient, data, cancellationToken);
        if (error != null)
        {
            Console.WriteLine($"Error: {error}");
            throw error;
        }
        return (response as TBalance)!;
    }
}