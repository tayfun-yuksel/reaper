namespace Reaper.Exchanges.Kucoin.Services;
public interface IBackTestService
{
    Task<decimal> TradeWithMultipleIndicatorsAsync(string symbol,
                                                   string startTime,
                                                   string? endTime,
                                                   int interval,
                                                   decimal tradeAmount,
                                                   string[] indicators,
                                                   CancellationToken cancellationToken);

    // Task<decimal> RSIAsync(string symbol, decimal tradeAmount, int interval, int period, CancellationToken cancellationToken);

    // Task<decimal> StochasticOscillatorAsync(string symbol, decimal tradeAmount, int interval, int period, CancellationToken cancellationToken);

    // Task<decimal> WilliamsRAsync(string symbol, decimal tradeAmount, int interval, int period, CancellationToken cancellationToken);

    // Task<decimal> CommodityChannelIndexAsync(string symbol, decimal tradeAmount, int interval, int period, CancellationToken cancellationToken);

    // Task<decimal> AverageTrueRangeAsync(string symbol, decimal tradeAmount, int interval, int period, CancellationToken cancellationToken);

    // Task<decimal> ChaikinMoneyFlowAsync(string symbol, decimal tradeAmount, int interval, int period, CancellationToken cancellationToken);

    // Task<decimal> OnBalanceVolumeAsync(string symbol, decimal tradeAmount, int interval, CancellationToken cancellationToken);

    // Task<decimal> AccumulationDistributionLineAsync(string symbol, decimal tradeAmount, int interval, CancellationToken cancellationToken);

    // Task<decimal> UltimateOscillatorAsync(string symbol, decimal tradeAmount, int interval, int period1, int period2, int period3, CancellationToken cancellationToken);

    // Task<decimal> AverageDirectionalIndexAsync(string symbol, decimal tradeAmount, int interval, int period, CancellationToken cancellationToken);

    // Task<decimal> MomentumAsync(string symbol, decimal tradeAmount, int interval, int period, CancellationToken cancellationToken);

    // Task<decimal> RateOfChangeAsync(string symbol, decimal tradeAmount, int interval, int period, CancellationToken cancellationToken);

    // Task<decimal> TrixAsync(string symbol, decimal tradeAmount, int interval, int period, CancellationToken cancellationToken);

    // Task<decimal> MassIndexAsync(string symbol, decimal tradeAmount, int interval, int period, CancellationToken cancellationToken);

    // Task<decimal> VortexIndicatorAsync(string symbol, decimal tradeAmount, int interval, int period, CancellationToken cancellationToken);

}