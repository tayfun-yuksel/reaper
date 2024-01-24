namespace Reaper.Exchanges.Kucoin.Services.Models;
public class KlineResponse
{
    public string Code { get; set; } = null!;
    public List<SpotKline> Data { get; set; } = null!;
}