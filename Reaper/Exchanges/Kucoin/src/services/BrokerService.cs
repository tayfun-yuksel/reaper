using System.Dynamic;
using Flurl.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Reaper.CommonLib.Interfaces;
using Reaper.Exchanges.Kucoin.Interfaces;
using Reaper.Exchanges.Kucoin.Services.Models;

namespace Reaper.Exchanges.Kucoin.Services;
public class BrokerService(IOptions<KucoinOptions> kucoinOptions,
    IMarketDataService marketDataService,
    IPositionService positionService,
    IOrderService orderService) : IBrokerService
{
    private readonly KucoinOptions _kucoinOptions = kucoinOptions.Value;


    internal async Task<Result<string>> PlaceMarketOrderAsync(string side, string symbol, decimal amount, CancellationToken cancellationToken)
    {
        var marketPrice = await marketDataService.GetSymbolPriceAsync(symbol, cancellationToken);
        var quantity = amount / marketPrice;
        using var flurlClient = CommonLib.Utils.FlurlExtensions.GetFlurlClient(_kucoinOptions.FuturesBaseUrl, true);

        var sellMarketFn = async (IFlurlClient client, object? requestData, CancellationToken cancellationToken) =>
            await client.Request()
                .AppendPathSegments("api", "v1", "orders")
                .SetQueryParams(requestData)
                .WithSignatureHeaders(_kucoinOptions, "POST", JsonConvert.SerializeObject(requestData))
                .PostJsonAsync(requestData, HttpCompletionOption.ResponseContentRead, cancellationToken)
                .ReceiveString();

        Result<string> result = await sellMarketFn.CallAsync(flurlClient, new
        {
            clientOid = Guid.NewGuid().ToString(),
            symbol = symbol.ToUpper(),
            leverage = 1,
            side = side,
            size = quantity,
            type = "market"
        }, cancellationToken);

        return result;
    }


    internal async Task WaitUntilActiveOrdersAreFilledAsync(string symbol, CancellationToken cancellationToken)
    {
        Result<string> result = await orderService.GetOrdersAsync(new Dictionary<string, object?>
        {
            { "symbol", symbol.ToUpper() },
            { "status", "active" }
        }, cancellationToken);

        if (result.Error != null)
        {
            throw new InvalidOperationException("Error getting active orders");
        }

        dynamic response = JsonConvert.DeserializeObject<ExpandoObject>(result.Data!);
        if (((IEnumerable<dynamic>)response.data.items).Any())
        {
            await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            await WaitUntilActiveOrdersAreFilledAsync(symbol, cancellationToken);
        }
    }


    internal async Task<decimal> GetDifferenceAsync(string position, string symbol, decimal amount, CancellationToken cancellationToken)
    {
        var positionAmount = await positionService.GetPositionAmountAsync(symbol, cancellationToken);
        var difference =  amount - positionAmount ;
        return difference;
    }




    public Task<bool> BuyLimitAsync(string symbol, decimal quantity, decimal price, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }


    public async Task<string> BuyMarketAsync(string symbol, decimal amount, CancellationToken cancellationToken)
    {
        var lastOrder = await PlaceMarketOrderAsync("buy", symbol, amount, cancellationToken);
        if (lastOrder.Error != null)
        {
            throw new InvalidOperationException("Error placing market order");
        }

        await WaitUntilActiveOrdersAreFilledAsync(symbol, cancellationToken);
        var difference = await GetDifferenceAsync("long", symbol, amount, cancellationToken);

        if (difference >= 1)
        {
            await BuyMarketAsync(symbol, difference, cancellationToken);
        }

        return lastOrder.Data!;
    }



    public Task<bool> SellLimitAsync(string symbol, decimal quantity, decimal price, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }




    public async Task<string> SellMarketAsync(string symbol, decimal amount, CancellationToken cancellationToken)
    {
        var lastOrder = await PlaceMarketOrderAsync("sell", symbol, amount, cancellationToken);
        if (lastOrder.Error != null)
        {
            throw new InvalidOperationException("Error placing market order");
        }

        await WaitUntilActiveOrdersAreFilledAsync(symbol, cancellationToken);
        var difference = await GetDifferenceAsync("short", symbol, amount, cancellationToken);

        if (difference >= 1)
        {
            await SellMarketAsync(symbol, difference, cancellationToken);
        }

        return lastOrder.Data!;
    }


}
