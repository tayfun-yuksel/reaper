using Flurl.Http;
using Reaper.CommonLib.Interfaces;
using Reaper.Exchanges.Kucoin.Services.Models;
using Microsoft.Extensions.Options;
using System.Dynamic;
using Newtonsoft.Json;

namespace Reaper.Exchanges.Kucoin.Services;
public class BalanceService(IOptions<KucoinOptions> kucoinOptions) : IBalanceService
{
    private readonly KucoinOptions _kucoinOptions = kucoinOptions.Value;

    public async Task<TBalance?> GetBalanceAsync<TBalance>(string? symbol, CancellationToken cancellationToken)
        where TBalance : class
    {
        using var flurlClient = CommonLib.Utils.FlurlExtensions.GetFlurlClient(_kucoinOptions.BaseUrl, true);

        var getBalanceFn = async (IFlurlClient client, object? requestData, CancellationToken cancellation) 
            => await client.Request()
                .AppendPathSegments("api", "v1", "accounts")
                .WithSignatureHeaders(_kucoinOptions, "GET")
                .GetAsync(HttpCompletionOption.ResponseContentRead, cancellation)
                .ReceiveString();

        Result<string> result = await getBalanceFn.CallAsync(flurlClient, null, cancellationToken);
        dynamic response = JsonConvert.DeserializeObject<ExpandoObject>(result.Data!);

        var symbolInfo = ((IEnumerable<dynamic>)response.data)
            .First(x => x.currency == (symbol?.ToUpper() ?? "USDT"));

        var balance = (symbolInfo.balance as TBalance) 
            ?? throw new InvalidOperationException(nameof(symbolInfo.balance));
        return balance;
    }
}