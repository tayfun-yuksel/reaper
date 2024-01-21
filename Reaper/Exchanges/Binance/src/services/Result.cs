
namespace Reaper.Exchanges.Binance.Services;
public class Result<TResponse>()
{
    public TResponse? Data { get; set; }
    public Exception? Error { get; set; }
}