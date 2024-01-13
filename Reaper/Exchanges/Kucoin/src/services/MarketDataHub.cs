using Microsoft.AspNetCore.SignalR;

namespace Reaper.Exchanges.Services.Kucoin;
public class MarketDataHub : Hub
{
    public async Task GetMarketDataAsync()
    {
        string data = "market-data";
        await Clients.All.SendAsync("ReceiveMarketData",  data);
    }
}
