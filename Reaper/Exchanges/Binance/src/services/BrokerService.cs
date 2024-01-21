
using System.Globalization;
using Flurl.Http;
using Microsoft.Extensions.Options;
using Reaper.CommonLib.Interfaces;
using Reaper.Exchanges.Binance.Interfaces;
using Reaper.Exchanges.Binance.Services.ApiModels;
using Reaper.Exchanges.Binance.Services.Configuration;

namespace Reaper.Exchanges.Binance.Services;
public class BrokerService(IMarketDataService marketDataService, IOptions<BinanceOptions> options) : IBrokerService
{
    private readonly IMarketDataService _marketDataService = marketDataService;
    private readonly BinanceOptions _binanceOptions = options.Value;



    public async Task<bool> BuyLimitAsync(string symbol, decimal quantity, decimal price, CancellationToken cancellationToken)
    {
        using var flurlClient = FlurlExtensions.GetFlurlClient(_binanceOptions);

        var parameters = new
        {
            symbol = symbol,
            side = "BUY",
            type = "LIMIT",
            timeInForce = "GTC",
            quantity = quantity,
            price = price
        };

        var buyLimitFn = async(IFlurlClient client, object? requestData, CancellationToken cancellation) =>
            await client.Request()
                .AppendPathSegments("api", "v3", "order")
                .WithHeader("X-MBX-APIKEY", _binanceOptions.ApiKey)
                .SetQueryParams(requestData)
                .WithSignedQueryParams(_binanceOptions.SecretKey, requestData)
                .PostAsync()
                .ReceiveString();
        Result<string> result = await buyLimitFn.CallAsync(flurlClient, parameters, cancellationToken);
        if (result.Error != null)
        {
            return false;
        }
        return true;
    }




    public async Task<bool> BuyMarketAsync(string symbol, decimal quantity, CancellationToken cancellationToken)
    {
        var exchangeInfo = await _marketDataService.GetSymbolExchangeInfoAsync(symbol, cancellationToken);
        var symbolPrice = await _marketDataService.GetSymbolPriceAsync(symbol, cancellationToken);
        var minNotional = decimal.Parse(exchangeInfo!.Symbols.Single(x => symbol.Equals(x.BaseAsset + x.QuoteAsset))
            .Filters.First(f => f.FilterType == FilterType.NOTIONAL.ToString()).MinNotional, CultureInfo.InvariantCulture);
        if (quantity * symbolPrice < minNotional)
        {
            throw new InvalidOperationException(@$"Quantity {quantity} is less than minNotional {minNotional}
                Minimum Quantity: {minNotional / symbolPrice}");
        }
        using var flurlClient = FlurlExtensions.GetFlurlClient(_binanceOptions);

        var parameters = new
        {
            symbol = symbol,
            side = "BUY",
            type = "MARKET",
            quantity = quantity
        };

        var buyLimitFn = async(IFlurlClient client, object? requestData, CancellationToken cancellation) =>
            await client.Request()
                .AppendPathSegments("api", "v3", "order")
                .WithHeader("X-MBX-APIKEY", _binanceOptions.ApiKey)
                .WithSignedQueryParams(_binanceOptions.SecretKey, requestData)
                .PostAsync()
                .ReceiveString();
        Result<string> result = await buyLimitFn.CallAsync(flurlClient, parameters, cancellationToken);
        if (result.Error != null)
        {
            return false;
        }
        return true;

    }




    public Task<bool> SellLimitAsync(string symbol, decimal quantity, decimal price, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> SellMarketAsync(string symbol, decimal quantity, CancellationToken cancellationToken)
    {
        using var flurlClient = FlurlExtensions.GetFlurlClient(_binanceOptions);
        var parameters = new
        {
            symbol = symbol,
            side = "SELL",
            type = "MARKET",
            quantity = quantity
        };
        var sellMarketFn = async(IFlurlClient client, object? requestData, CancellationToken cancellation) =>
            await client.Request()
                .AppendPathSegments("api", "v3", "order")
                .WithHeader("X-MBX-APIKEY", _binanceOptions.ApiKey)
                .WithSignedQueryParams(_binanceOptions.SecretKey, requestData)
                .PostAsync()
                .ReceiveString();

        Result<string> result = await sellMarketFn.CallAsync(flurlClient, parameters, cancellationToken);
        if (result.Error != null)
        {
            return false;
        }
        return true;
    }
}