
namespace Reaper.Exchanges.Kucoin.Services;
public class FuturesKline
{
    public long Time { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal HighestPrice { get; set; }
    public decimal LowestPrice { get; set; }
    public decimal ClosePrice { get; set; }
    public decimal TradingVolume { get; set; }
    
}