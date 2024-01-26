namespace Reaper.Exchanges.Kucoin.Services.Models;
public class AccountBalance
{
    public string code { get; set; } = string.Empty;
    public List<BalanceInfo> Data { get; set; }  = [];
}
