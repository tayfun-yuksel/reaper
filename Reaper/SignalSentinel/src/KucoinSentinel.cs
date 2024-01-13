using Microsoft.AspNetCore.SignalR.Client;

namespace Reaper.SignalSentinel;
public class KucoinSentinel
{
    private HubConnection _hubConnection;

    public async void InitHubConnection()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl("https://localhost:5001/kucoin")
            .Build();

        _hubConnection.On<string>("ReceiveMarketData", HandleReceivedMarketData);

        await _hubConnection.StartAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Console.WriteLine("There was an error opening the connection:{0}", task.Exception.GetBaseException());
            }
        });
    }

    public async void CloseHubConnection()
    {
        await _hubConnection.StopAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Console.WriteLine("There was an error closing the connection:{0}", task.Exception.GetBaseException());
            }
            else
            {
                Console.WriteLine("Connection closed.");
            }
        });
    }

    public static void HandleReceivedMarketData(string data)
    {
        Console.WriteLine("Received Market Data: " + data);
        // Process the received market data
    }

    public async Task GetMarketDataAsync()
    {
        if (_hubConnection.State == HubConnectionState.Connected)
        {
            await _hubConnection.SendAsync("GetMarketData");
        }
        else
        {
            Console.WriteLine("Hub connection is not established.");
        }
    }
}