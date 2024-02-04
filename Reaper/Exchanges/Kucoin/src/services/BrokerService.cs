using System.Dynamic;
using Flurl.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Reaper.CommonLib.Interfaces;
using Reaper.Exchanges.Kucoin.Services.Models;

namespace Reaper.Exchanges.Kucoin.Services;
public class BrokerService(IOptions<KucoinOptions> kucoinOptions,
    IMarketDataService marketDataService,
    IOrderService orderService) : IBrokerService
{
    private readonly KucoinOptions _kucoinOptions = kucoinOptions.Value;

    /// <summary>
    /// Places a market order and returns the filled value
    /// </summary>
    internal Func<string, string, decimal, CancellationToken, Task<Result<string>>> PlaceMarketOrderAsync =>
        async(string side, string symbol, decimal amount, CancellationToken cancellationToken) =>
    {
        var priceResult = await marketDataService.GetSymbolPriceAsync(symbol, cancellationToken);
        if (priceResult.Error != null)
        {
            return new() { Error = priceResult.Error };
        }
        
        var marketPrice = priceResult.Data!;
        var quantity = amount / marketPrice;

        using var flurlClient = CommonLib.Utils.FlurlExtensions.GetFlurlClient(RLogger.HttpLog, _kucoinOptions.FuturesBaseUrl, true);

        var queryParams = new
        {
            clientOid = Guid.NewGuid().ToString(),
            symbol = symbol.ToUpper(),
            leverage = 1,
            side = side,
            size = quantity,
            type = "market"
        };

        var placeOrder = async () =>
            await flurlClient.Request()
                .AppendPathSegments("api", "v1", "orders")
                .SetQueryParams(queryParams)
                .WithSignatureHeaders(_kucoinOptions, "POST", JsonConvert.SerializeObject(queryParams))
                .PostJsonAsync(queryParams, HttpCompletionOption.ResponseContentRead, cancellationToken)
                .ReceiveString();

        Result<string> result = await placeOrder
            .WithErrorPolicy(RetryPolicies.HttpErrorLogAndRetryPolicy)
            .CallAsync();

        if (result.Error != null)
        {
            return new() { Error = result.Error };
        }

        dynamic response = JsonConvert.DeserializeObject<ExpandoObject>(result.Data!);
        var orderId = response.data.orderId;
        return new() { Data = orderId };
    };



    internal Func<string, CancellationToken, Task> WaitUntilActiveOrdersAreFilledAsync =>
        async(string symbol, CancellationToken cancellationToken) =>
    {
        Result<IEnumerable<string>> result = await orderService.GetActiveOrdersAsync(symbol, cancellationToken);

        if (result.Error != null)
        {
            throw new InvalidOperationException("Error getting active orders");
        }

        if (result.Data!.Any())
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
        var orderId = await PlaceMarketOrderAsync("buy", symbol, amount, cancellationToken);
        if (orderId.Error != null)
        {
            return new() { Error = orderId.Error };
        }
        var filledValue = await orderService.GetOrderAmountAsync
            (orderId.Data!, cancellationToken);

        if (filledValue.Error != null)
        {
            return new() { Error = filledValue.Error };
        }

        await WaitUntilActiveOrdersAreFilledAsync(symbol, cancellationToken);
        var difference = amount - filledValue.Data!;

        if (difference >= 1)
        {
            RLogger.AppLog.Information($"Buying remaining amount {difference} of {symbol}");
            await BuyMarketAsync(symbol, difference, cancellationToken);
        }
        return new() { Data = "done" };
    }


    public async Task<Result<string>> SellMarketAsync(string symbol, decimal amount, CancellationToken cancellationToken)
    {
        var orderId = await PlaceMarketOrderAsync("sell", symbol, amount, cancellationToken);
        if (orderId.Error != null)
        {
            throw new InvalidOperationException("Error placing market order");
        }

        var filledValue = await orderService.GetOrderAmountAsync(orderId.Data!, cancellationToken);
        await WaitUntilActiveOrdersAreFilledAsync(symbol, cancellationToken);
        var difference = amount - filledValue.Data!;

        if (difference >= 1)
        {
            await SellMarketAsync(symbol, difference, cancellationToken);
        }
        return new() { Data = "done" };
    }

    Task<Result<string>> IBrokerService.SellLimitAsync(string symbol, decimal quantity, decimal price, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
