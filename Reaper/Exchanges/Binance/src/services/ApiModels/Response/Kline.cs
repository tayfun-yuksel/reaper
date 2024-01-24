namespace Reaper.Exchanges.Binance.Services.ApiModels;
public class Kline
{
  // Using long for time values as they are likely Unix timestamps in milliseconds
    public long OpenTime { get; set; }
    public string Open { get; set; } = string.Empty;
    public string High { get; set; } = string.Empty;
    public string Low { get; set; } = string.Empty;
    public string Close { get; set; } = string.Empty;
    public string Volume { get; set; } = string.Empty;
    public long CloseTime { get; set; } 
    public string BaseAssetVolume { get; set; } = string.Empty;
    public int NumberOfTrades { get; set; } 
    public string TakerBuyVolume { get; set; } = string.Empty;
    public string TakerBuyBaseAssetVolume { get; set; } = string.Empty;
    // Ignored property is not included as it's not needed   
}