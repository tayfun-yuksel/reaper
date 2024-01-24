
namespace Reaper.Exchanges.Kucoin.Services.Models;
public class Result<TResponse>()
{
    public TResponse? Data { get; set; }
    public Exception? Error { get; set; }
}