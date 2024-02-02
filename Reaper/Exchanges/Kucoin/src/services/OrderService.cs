using Flurl.Http;
using Microsoft.Extensions.Options;
using Reaper.CommonLib.Interfaces;
using Reaper.Exchanges.Kucoin.Services.Models;

namespace Reaper.Exchanges.Kucoin.Services;
public class OrderService(IOptions<KucoinOptions> kucoinOptions) : IOrderService
{
    private readonly KucoinOptions _kucoinOptions = kucoinOptions.Value;

    public async Task<Result<string>> GetOrdersAsync(IDictionary<string, object?> parameters, CancellationToken cancellationToken)
    {
        using var flurlClient = CommonLib.Utils.FlurlExtensions.GetFlurlClient(_kucoinOptions.FuturesBaseUrl, true);

        var getOrderDetailsFn = async(IFlurlClient client, object? requestData, CancellationToken cancellation) =>
            await client.Request()
                .AppendPathSegments("api", "v1", "orders")
                .SetQueryParams(requestData)
                .WithSignatureHeaders(_kucoinOptions, "GET") 
                .GetAsync(HttpCompletionOption.ResponseContentRead, cancellation)
                .ReceiveString();

        Result<string> result = await getOrderDetailsFn.CallAsync(flurlClient, parameters, cancellationToken);
        return result;
    }

    public Task<Result<IEnumerable<TOrder>>> GetOrdersBySymbolAsync<TOrder>(string symbol, DateTime from, DateTime to)
        where TOrder : class
    {
        throw new NotImplementedException();
    }

}