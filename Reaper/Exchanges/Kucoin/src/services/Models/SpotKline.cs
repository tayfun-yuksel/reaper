namespace Reaper.Exchanges.Kucoin.Services.Models;
public class SpotKline
{
  // Using long for time values as they are likely Unix timestamps in milliseconds
    public string OpenTime { get; set; } = string.Empty;
    public string Open { get; set; } = string.Empty;
    public string High { get; set; } = string.Empty;
    public string Low { get; set; } = string.Empty;
    public string Close { get; set; } = string.Empty;
    public string Volume { get; set; } = string.Empty;
    public string TransactionAmount { get; set; }  = string.Empty;
}