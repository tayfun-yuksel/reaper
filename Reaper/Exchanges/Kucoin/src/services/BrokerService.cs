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
    /// Places a market order and returns the order id
    /// </summary>
    internal async Task<Result<string>> PlaceMarketOrderAsync(
            string side,
            string type,
            string symbol,
            decimal amount,
            decimal? limitPrice,
            CancellationToken cancellationToken)
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
            price = limitPrice,
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

        RLogger.AppLog.Information(@$"Order({type}) placed for {symbol} 
                    at limitPrice: {limitPrice} at {DateTime.UtcNow}");

        dynamic response = JsonConvert.DeserializeObject<ExpandoObject>(result.Data!);
        var orderId = response.data.orderId;
        return new() { Data = orderId };
    }



    internal async Task WaitUntilActiveOrdersAreFilledAsync(string symbol, CancellationToken cancellationToken)
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
    }


    internal async Task<Result<string>> EnsureTransactionCompleted(
        string symbol,
        decimal amount,
        decimal limitPrice,
        string orderId,
        Func<string, decimal, CancellationToken, Task>? marketAction,
        Func<string, decimal, decimal, CancellationToken, Task>? limitAction,
        CancellationToken cancellationToken)
    {
        var msgTemplate = @$"{nameof(BrokerService)}_{nameof(EnsureTransactionCompleted)}: ";

        var filledValue = await orderService.GetOrderAmountAsync
            (orderId, cancellationToken);

        if (filledValue.Error != null)
        {
            return new() { Error = filledValue.Error };
        }

        await WaitUntilActiveOrdersAreFilledAsync(symbol, cancellationToken);
        var amountToFill = amount - filledValue.Data!;
        RLogger.AppLog.Information(msgTemplate + $"target-amount: {amount}");
        RLogger.AppLog.Information(msgTemplate + $"amount filled: {filledValue.Data}");
        RLogger.AppLog.Information(msgTemplate + $"amount to fill: {amountToFill}");

        if (amountToFill >= 1)
        {
            RLogger.AppLog.Information(msgTemplate + $"buying remaining amount {amountToFill} of {symbol}");
            if (marketAction != null)
            {
                await marketAction(symbol, amountToFill, cancellationToken);
            }
            else
            {
                await limitAction!(symbol, amountToFill, limitPrice, cancellationToken);
            }
        }
        RLogger.AppLog.Information(msgTemplate + "transaction completed");
        return new() { Data = "done" };

    }


    public async Task<Result<string>> BuyLimitAsync(
        string symbol,
        decimal amount,
        decimal limitPrice,
        CancellationToken cancellationToken)
    {
        var orderId = await PlaceMarketOrderAsync(
            _side_.buy,
            _type_.limit,
            symbol,
            amount,
            limitPrice,
            cancellationToken);

        if (orderId.Error != null)
        {
            return new() { Error = orderId.Error };
        }

        RLogger.AppLog.Information(@$"{nameof(BrokerService)}: 
                Limit order buy placed for {symbol} at {limitPrice} at {DateTime.UtcNow}");


        return await EnsureTransactionCompleted(
            symbol,
            amount,
            limitPrice,
            orderId.Data!,
            marketAction: null,
            limitAction: BuyLimitAsync,
            cancellationToken);
    }


    public async Task<Result<string>> BuyMarketAsync(
        string symbol,
        decimal amount,
        CancellationToken cancellationToken)
    {
        var orderId = await PlaceMarketOrderAsync(
            _side_.buy,
            _type_.market,
            symbol,
            amount,
            limitPrice: null,
            cancellationToken);

        if (orderId.Error != null)
        {
            return new() { Error = orderId.Error };
        }

        RLogger.AppLog.Information(@$"{nameof(BrokerService)}: 
                Market order buy placed for {symbol} at {DateTime.UtcNow}");

        return await EnsureTransactionCompleted(
            symbol,
            amount,
            limitPrice: 0,
            orderId.Data!,
            marketAction: BuyMarketAsync,
            limitAction: null,
            cancellationToken);
    }



    public async Task<Result<string>> SellLimitAsync(
        string symbol,
        decimal amount,
        decimal limitPrice,
        CancellationToken cancellationToken)
    {
        var orderId = await PlaceMarketOrderAsync(
            _side_.sell,
            _type_.limit,
            symbol,
            amount,
            limitPrice,
            cancellationToken);

        if (orderId.Error != null)
        {
            throw new InvalidOperationException("Error placing market order");
        }

        RLogger.AppLog.Information(@$"{nameof(BrokerService)}: 
                Limit order sell placed for {symbol} at {limitPrice} at {DateTime.UtcNow}");

        return await EnsureTransactionCompleted(
            symbol,
            amount,
            limitPrice,
            orderId.Data!,
            marketAction: null,
            limitAction: SellLimitAsync,
            cancellationToken);
    }



    public async Task<Result<string>> SellMarketAsync(
        string symbol,
        decimal amount,
        CancellationToken cancellationToken)
    {
        var orderId = await PlaceMarketOrderAsync(
            _side_.sell,
            _type_.market,
            symbol,
            amount,
            limitPrice: null,
            cancellationToken);

        if (orderId.Error != null)
        {
            throw new InvalidOperationException("Error placing market order");
        }

        RLogger.AppLog.Information(@$"{nameof(BrokerService)}: 
                Market order sell placed for {symbol} at {DateTime.UtcNow}");

        return await EnsureTransactionCompleted(
            symbol,
            amount,
            limitPrice: 0,
            orderId.Data!,
            marketAction: SellMarketAsync,
            limitAction: null,
            cancellationToken);
    }
}
