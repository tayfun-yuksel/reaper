using Flurl.Http;
using Microsoft.Extensions.Configuration;
using Reaper.CommonLib.Interfaces;
using Reaper.Kucoin.Services.Models;

namespace Reaper.Exchanges.Services.Kucoin;
public class OrderService(IConfiguration configuration) : IOrderService
{
    private readonly IConfiguration _configuration = configuration;

    public async  Task<IEnumerable<TOrder>> GetOrdersBySymbolAsync<TOrder>(string symbol, DateTime from, DateTime to)
        where TOrder : class
    {
        var method = "GET";
        using var flurlClient = FlurlExtensions.GetFlurlClient();
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
                .WithSignatureHeaders(_configuration, method)
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