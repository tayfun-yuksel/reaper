using System.Dynamic;
using Flurl.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using Reaper.CommonLib.Interfaces;
using Reaper.Exchanges.Kucoin.Interfaces;
using Reaper.Exchanges.Kucoin.Services.Models;

namespace Reaper.Exchanges.Kucoin.Services;
public class BrokerService(IOptions<KucoinOptions> kucoinOptions,
    IMarketDataService marketDataService,
    IOrderService orderService) : IBrokerService
{
    private readonly KucoinOptions _kucoinOptions = kucoinOptions.Value;
    private readonly string OrderNotExistError_ = "error.getOrder.orderNotExist";


    private static AsyncRetryPolicy<Result<string>> ErrorMessageHandlePolicy => Policy
        .HandleResult<Result<string>>(r =>
        {
            if (r.Error != null)
            {
                Utils.Print("Error while getting order details", r.Error.Message, ConsoleColor.Red);
                return true;
            }
            dynamic response = JsonConvert.DeserializeObject<ExpandoObject>(r.Data!);
            var errorToCheck = "error.getOrder.orderNotExist";
            return r.Data!.Contains(errorToCheck);
        })
        .WaitAndRetryAsync(
            3,
            retryAttempt => TimeSpan.FromSeconds(2),
            onRetry: (outCome, timeSpan, retryCount, context) => 
            {
                Utils.Print("Retrying", $"after {timeSpan.Seconds} seconds due to: {outCome.Result.Data}", ConsoleColor.Yellow);
                Utils.Print("RetryCount", retryCount.ToString(), ConsoleColor.Red);
            });




    internal Func<string, CancellationToken, Task<Result<decimal>>> GetFilledValueAsync =>
        async(string orderId, CancellationToken cancellationToken) =>
    {
        using var flurlClient = CommonLib.Utils.FlurlExtensions.GetFlurlClient(_kucoinOptions.FuturesBaseUrl, true);

        var getOrderDetailsFn = async (IFlurlClient client, object? requestData, CancellationToken cancellationToken) =>
            await client.Request()
                .AppendPathSegments("api", "v1", "orders", orderId)
                .WithSignatureHeaders(_kucoinOptions, "GET")
                .GetAsync()
                .ReceiveString();

        Result<string> result = await getOrderDetailsFn.CallAsync(flurlClient, null, cancellationToken);

        if (result.Error != null)
        {
            Utils.Print("Error while getting order details", result.Error!.Message, ConsoleColor.Red);
        }
        else if (!result.Data!.Contains("data"))
        {
            Utils.Print("Error while getting order details", result.Data!, ConsoleColor.Red);
        }

        dynamic response = JsonConvert.DeserializeObject<ExpandoObject>(result.Data!);
        var filledValue = decimal.Parse(response.data.filledValue);
        return filledValue;
    };



    /// <summary>
    /// Places a market order and returns the filled value
    /// </summary>
    internal Func<string, string, decimal, CancellationToken, Task<Result<decimal>>> PlaceMarketOrderAsync =>
        async(string side, string symbol, decimal amount, CancellationToken cancellationToken) =>
    {
        var priceResult = await marketDataService.GetSymbolPriceAsync(symbol, cancellationToken);
        if (priceResult.Error != null)
        {
            return new() { Error = priceResult.Error };
        }
        
        var marketPrice = priceResult.Data!;
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

        if (result.Error != null)
        {
            return new() { Error = result.Error };
        }

        Utils.Print($"Placed market order:", result.Data!, ConsoleColor.Green);
        dynamic response = JsonConvert.DeserializeObject<ExpandoObject>(result.Data!);
        string orderId = response.data.orderId;

        var filledValue = await GetFilledValueAsync(orderId, cancellationToken);
        Utils.Print("Order details:", filledValue.Data.ToString(), ConsoleColor.Green);
        return filledValue;
    };



    internal Func<string, CancellationToken, Task> WaitUntilActiveOrdersAreFilledAsync =>
        async(string symbol, CancellationToken cancellationToken) =>
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
    };



    public Task<Result<string>> BuyLimitAsync(string symbol, decimal quantity, decimal price, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }



    public async Task<Result<string>> BuyMarketAsync(string symbol, decimal amount, CancellationToken cancellationToken)
    {
        var filledvalue = await PlaceMarketOrderAsync("buy", symbol, amount, cancellationToken);
        if (filledvalue.Error != null)
        {
            return new() { Error = filledvalue.Error };
        }

        await WaitUntilActiveOrdersAreFilledAsync(symbol, cancellationToken);
        var difference = amount - filledvalue.Data!;

        if (difference >= 1)
        {
            await BuyMarketAsync(symbol, difference, cancellationToken);
        }
        return new() { Data = "done" };
    }

    public Task<Result<string>> SellLimitAsync(string symbol, decimal quantity, decimal price, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<string>> SellMarketAsync(string symbol, decimal amount, CancellationToken cancellationToken)
    {
        var filledValue = await PlaceMarketOrderAsync("sell", symbol, amount, cancellationToken);
        if (filledValue.Error != null)
        {
            throw new InvalidOperationException("Error placing market order");
        }

        await WaitUntilActiveOrdersAreFilledAsync(symbol, cancellationToken);
        var difference = amount - filledValue.Data!;

        if (difference >= 1)
        {
            await SellMarketAsync(symbol, difference, cancellationToken);
        }
        return new() { Data = "done" };
    }


}
