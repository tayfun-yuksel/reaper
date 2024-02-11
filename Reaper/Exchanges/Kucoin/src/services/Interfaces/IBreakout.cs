namespace Reaper.Exchanges.Kucoin.Interfaces;
public interface IBreakout
{
    Task PrepareDataForPlottingAsync(string symbol, string startTime, int interval);
}