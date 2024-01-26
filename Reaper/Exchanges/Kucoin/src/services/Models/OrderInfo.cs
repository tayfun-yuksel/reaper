namespace Reaper.Exchanges.Kucoin.Services.Models;
public class OrderInfo
{
    public string OrderId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string Side { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Size { get; set; }
    public decimal Funds { get; set; }
    public decimal FilledSize { get; set; }
    public decimal FilledFunds { get; set; }
    public DateTime CreatedAt { get; set; }
}