using System.Text.Json;
using Flurl.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Reaper.CommonLib.Interfaces;
using Reaper.Exchanges.Kucoin.Services;
using Reaper.Exchanges.Kucoin.Services.Models;

namespace Reaper.Exchanges.Services.Kucoin;
public class BrokerService(IOptions<KucoinOptions> kucoinOptions) : IBrokerService
{
    private readonly KucoinOptions _kucoinOptions = kucoinOptions.Value;
    public async Task<decimal> GetSpotMinimumTradeSizeAsync(string symbol, CancellationToken cancellationToken)
    {
        var endpoint = "/api/v2/symbols";
        using var flurlClient = CommonLib.Utils.FlurlExtensions.GetFlurlClient(_kucoinOptions.BaseUrl, true); 
        var parameters = new
        {
            symbol = symbol
        };

        var getMinTradeSizeFn = async (IFlurlClient client, object? requestData, CancellationToken cancellationToken) =>
            await client.Request()
                .AppendPathSegments(endpoint)
                .GetAsync(HttpCompletionOption.ResponseContentRead, cancellationToken)
                .ReceiveJson<dynamic>();
        Result<dynamic> result = await getMinTradeSizeFn.CallAsync(flurlClient, parameters, cancellationToken);
        if (result.Error != null)
        {
            return 0;
        }
        return result.Data!.data[0].baseMinSize;
    }

    public Task<bool> BuyLimitAsync(string symbol, decimal quantity, decimal price, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> BuyMarketAsync(string symbol, decimal quantity, CancellationToken cancellationToken)
    {
        // using var flurlClient = FlurlExtensions.GetFlurlClient();
        // var futuresBuyMarketFn = async (IFlurlClient client, object? requestData, CancellationToken cancellationToken) =>
        //     await client.Request()
        //         .AppendPathSegments("api", "v1", "trade", "order", "market")
        //         .SetQueryParams(new
        //         {
        //             symbol = symbol,
        //             side = "buy",
        //             size = quantity,
        //             type = "market"
        //         })
        //         .WithSignatureHeaders(_configuration, "POST", JsonSerializer.Serialize(requestData))
        //         .PostAsync(HttpCompletionOption.ResponseContentRead, cancellationToken)
        //         .ReceiveJson<dynamic>();
        // Result<dynamic> result = await buyMarketFn.CallAsync(flurlClient, parameters, cancellationToken);
        // if (result.Error != null)
        // {
        //     return false;
        // }
        // Console.WriteLine($"Buy market order result: {result.Data}");
        return true;
    }

    public Task<bool> SellLimitAsync(string symbol, decimal quantity, decimal price, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<bool> SellMarketAsync(string symbol, decimal quantity, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
