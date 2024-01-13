namespace Reaper.Kucoin.Services.Models;
public class AccountBalance
{
    public string Code { get; set; } = string.Empty;
    public List<BalanceInfo> Data { get; set; }  = Array.Empty<BalanceInfo>().ToList();
}
