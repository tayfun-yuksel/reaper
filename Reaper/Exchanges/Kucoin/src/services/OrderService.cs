using Flurl.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Reaper.CommonLib.Interfaces;
using Reaper.Exchanges.Kucoin.Services.Models;
using Reaper.Kucoin.Services.Models;

namespace Reaper.Exchanges.Kucoin.Services;
public class OrderService(IOptions<KucoinOptions> kucoinOptions) : IOrderService
{
    private readonly KucoinOptions _kucoinOptions = kucoinOptions.Value;

    public async  Task<IEnumerable<TOrder>> GetOrdersBySymbolAsync<TOrder>(string symbol, DateTime from, DateTime to)
        where TOrder : class
    {
        var method = "GET";
        using var flurlClient = CommonLib.Utils.FlurlExtensions.GetFlurlClient(_kucoinOptions.BaseUrl, true);
        try
        {
            var body = new
            {
                symbol = symbol,
                startAt = ((DateTimeOffset)(from.ToUniversalTime())).ToUnixTimeSeconds(),
                endAt = ((DateTimeOffset)(to.ToUniversalTime())).ToUnixTimeSeconds(),
            };
            var response = await flurlClient.Request()
                .AppendPathSegment("orders") 
                // .SetQueryParams(body)
                .WithSignatureHeaders(_kucoinOptions, method)
                .GetAsync()
                .ReceiveJson<OrdersResponse>();
            var filteredOrders = response.Data.Items!.Where(x => x.Symbol == symbol).ToList();
            return (IEnumerable<TOrder>)filteredOrders;
        }
        catch (FlurlHttpException ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            throw;
        }
    }

}