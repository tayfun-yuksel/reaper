namespace Reaper.Kucoin.Services.Models;
public class BalanceInfo
{
    public string Id { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Balance { get; set; } = string.Empty;
    public string Available { get; set; } = string.Empty;
    public string Holds { get; set; } = string.Empty;
}