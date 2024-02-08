using System.Dynamic;
using Flurl.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Reaper.CommonLib.Interfaces;
using Reaper.Exchanges.Kucoin.Services.Models;

namespace Reaper.Exchanges.Kucoin.Services;
public class OrderService(IOptions<KucoinOptions> kucoinOptions) : IOrderService
{
    private readonly KucoinOptions _kucoinOptions = kucoinOptions.Value;
    private static readonly string OrderNotExists_ = "error.getOrder.orderNotExist";
    private static readonly string ContractParameterInvalid_ = "Contract parameter invalid.";


    public async Task<Result<decimal>> GetOrderAmountAsync(string orderId, CancellationToken cancellationToken)
    {
        using var flurlClient = FlurlExtensions.GetHttpClient(_kucoinOptions);

        var getOrderDetailsFn = async() => await flurlClient.Request()
                .AppendPathSegments("api", "v1", "orders", orderId)
                .WithSignatureHeaders(_kucoinOptions, "GET") 
                .GetAsync(HttpCompletionOption.ResponseContentRead, cancellationToken)
                .ReceiveString();

        Result<string> result = await getOrderDetailsFn
            .WithErrorPolicy(RetryPolicies.HttpErrorLogAndRetryPolicy)
            .WithResponsePolicy(RetryPolicies.GetErrorMessageHandlePolicy(OrderNotExists_))
            .CallAsync();

        if (result.Error != null)
        {
            return new() { Error = result.Error };
        }

        dynamic response = JsonConvert.DeserializeObject<ExpandoObject>(result.Data!);
        decimal orderValue = decimal.Parse(response.data.filledValue);
        return new() { Data = orderValue };
    }


    public async Task<Result<IEnumerable<string>>> GetActiveOrdersAsync(
        string symbol,
        CancellationToken cancellationToken)
    {
        using var flurlClient = FlurlExtensions.GetHttpClient(_kucoinOptions);

        var getActiveOrdersFn = async() => await flurlClient.Request()
                .AppendPathSegments("api", "v1", "orders")
                .SetQueryParams(new
                {
                    symbol = symbol.ToUpper(),
                    status = "active"
                })
                .WithSignatureHeaders(_kucoinOptions, "GET") 
                .GetAsync(HttpCompletionOption.ResponseContentRead, cancellationToken)
                .ReceiveString();

        Result<string> result = await getActiveOrdersFn
            .WithErrorPolicy(RetryPolicies.HttpErrorLogAndRetryPolicy)
            .WithResponsePolicy(RetryPolicies.GetErrorMessageHandlePolicy(ContractParameterInvalid_))
            .CallAsync();

        if (result.Error != null)
        {
            return new() { Error = result.Error };
        }

        dynamic response = JsonConvert.DeserializeObject<ExpandoObject>(result.Data!);
        return new() 
        { 
            Data = ((IEnumerable<dynamic>)response.data.items).Select(x => (string)x.orderId)
        };
    }

}