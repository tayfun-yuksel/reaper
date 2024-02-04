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
    private (string market, string limit) _type_ = ("market", "limit");
    private (string buy, string sell) _side_ = ("buy", "sell");

    /// <summary>
    /// Places a market order and returns the filled value
    /// </summary>
    internal Func<string, string, string, decimal, CancellationToken, Task<Result<string>>> PlaceMarketOrderAsync =>
        async(string side, string type, string symbol, decimal amount, CancellationToken cancellationToken) =>
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
            type = type
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
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            await WaitUntilActiveOrdersAreFilledAsync(symbol, cancellationToken);
        }
    };


    internal async Task<Result<string>> EnsureTransactionCompleted(
        string symbol,
        Func<string, decimal, CancellationToken, Task> action,
        decimal amount,
        string orderId,
        CancellationToken cancellationToken)
    {
        var filledValue = await orderService.GetOrderAmountAsync
            (orderId, cancellationToken);

        if (filledValue.Error != null)
        {
            return new() { Error = filledValue.Error };
        }

        await WaitUntilActiveOrdersAreFilledAsync(symbol, cancellationToken);
        var difference = amount - filledValue.Data!;

        if (difference >= 1)
        {
            RLogger.AppLog.Information($"Buying remaining amount {difference} of {symbol}");
            await action(symbol, difference, cancellationToken);
        }
        return new() { Data = "done" };

    }


    public async Task<Result<string>> BuyLimitAsync(string symbol, decimal amount, CancellationToken cancellationToken)
    {
        var orderId = await PlaceMarketOrderAsync(_side_.buy, _type_.limit, symbol, amount, cancellationToken);
        if (orderId.Error != null)
        {
            return new() { Error = orderId.Error };
        }

        return await EnsureTransactionCompleted(
            symbol,
            BuyMarketAsync,
            amount,
            orderId.Data!,
            cancellationToken);
    }

    public async Task<Result<string>> BuyMarketAsync(string symbol, decimal amount, CancellationToken cancellationToken)
    {
        var orderId = await PlaceMarketOrderAsync(_side_.buy, _type_.market, symbol, amount, cancellationToken);
        if (orderId.Error != null)
        {
            return new() { Error = orderId.Error };
        }

        return await EnsureTransactionCompleted(
            symbol,
            BuyMarketAsync,
            amount,
            orderId.Data!,
            cancellationToken);
    }

    public async Task<Result<string>> SellLimitAsync(string symbol, decimal amount, CancellationToken cancellationToken)
    {
        var orderId = await PlaceMarketOrderAsync(_side_.sell, _type_.limit, symbol, amount, cancellationToken);
        if (orderId.Error != null)
        {
            throw new InvalidOperationException("Error placing market order");
        }
        return await EnsureTransactionCompleted(
            symbol,
            SellLimitAsync,
            amount,
            orderId.Data!,
            cancellationToken);
    }

    public async Task<Result<string>> SellMarketAsync(string symbol, decimal amount, CancellationToken cancellationToken)
    {
        var orderId = await PlaceMarketOrderAsync(_side_.sell, _type_.market, symbol, amount, cancellationToken);
        if (orderId.Error != null)
        {
            throw new InvalidOperationException("Error placing market order");
        }

        return await EnsureTransactionCompleted(
            symbol,
            SellMarketAsync,
            amount,
            orderId.Data!,
            cancellationToken);
    }
}
