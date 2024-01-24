using Reaper.Kucoin.Services.Models;
using Flurl.Http;
using Reaper.CommonLib.Interfaces;
using Microsoft.Extensions.Configuration;
using Reaper.Exchanges.Kucoin.Services.Models;
using Microsoft.Extensions.Options;

namespace Reaper.Exchanges.Kucoin.Services;
public class BalanceService(IOptions<KucoinOptions> kucoinOptions) : IBalanceService
{
    private readonly KucoinOptions _kucoinOptions = kucoinOptions.Value;

    public async Task<TBalance?> GetBalanceAsync<TBalance>(string symbol, CancellationToken cancellationToken)
        where TBalance : class
    {
        var method = "GET";
        using var flurlClient = CommonLib.Utils.FlurlExtensions.GetFlurlClient(_kucoinOptions.BaseUrl, true);
        var response = await flurlClient.Request()
            .AppendPathSegments("api", "v1", "accounts")
            .WithSignatureHeaders(_kucoinOptions, method)
            .GetAsync(cancellationToken: cancellationToken)
            .ReceiveJson<AccountBalance>();
        var usdtBalance = response.Data.First(x => x.Currency == symbol);
        Console.WriteLine($"{symbol} balance: {usdtBalance.Balance}"); 
        return (usdtBalance as TBalance) 
            ?? throw new InvalidOperationException(nameof(usdtBalance));
    }
}