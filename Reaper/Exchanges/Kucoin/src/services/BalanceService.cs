using Reaper.Kucoin.Services.Models;
using Flurl.Http;
using Reaper.CommonLib.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Reaper.Exchanges.Services.Kucoin;
public class BalanceService(IConfiguration configuration) : IBalanceService
{
    private readonly IConfiguration _configuration = configuration;

    public async Task<TBalance> GetBalanceAsync<TBalance>(CancellationToken cancellationToken)
        where TBalance : class
    {
        var method = "GET";
        using var flurlClient = FlurlExtensions.GetFlurlClient();
        var response = await flurlClient.Request()
            .AppendPathSegment("accounts")
            .WithSignatureHeaders(_configuration, method)
            .GetAsync()
            .ReceiveJson<AccountBalance>();
        var usdtBalance = response.Data.First(x => x.Currency == "USDT");
        Console.WriteLine($"USDT balance: {usdtBalance.Balance}"); 
        return (usdtBalance as TBalance) 
            ?? throw new InvalidOperationException(nameof(usdtBalance));
    }
}