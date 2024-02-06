namespace Reaper.Exchanges.Kucoin.Services;
public class FuturesKlineResponse
{
    public string Code { get; set; } = string.Empty;
    public IList<IList<decimal>> Data { get; set; } = [];
}