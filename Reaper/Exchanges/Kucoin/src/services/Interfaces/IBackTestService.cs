namespace Reaper.Exchanges.Kucoin.Services;
public interface IBackTestService
{
    Task<decimal> TilsonT3Async(string symbol,
                                string startTime,
                                string? endTime,
                                int interval,
                                decimal tradeAmount,
                                decimal volumeFactor,
                                CancellationToken cancellationToken);
    Task<decimal> MACDAsync(string symbol,
                            string startTime,
                            string? endTime,
                            int interval,
                            decimal tradeAmount,
                            int shortPeriod,
                            int longPeriod,
                            int smoothLine,
                            CancellationToken cancellationToken);
    Task<decimal> BollingerBandsAsync(string symbol,
                                     string startTime,
                                     string? endTime,
                                     int interval,
                                     decimal tradeAmount,
                                     int period,
                                     decimal deviationMultiplier,
                                     CancellationToken cancellationToken);

    Task<decimal> BollingerBandsAndTilsonT3Async(string symbol,
                                                 string startTime,
                                                 string? endTime,
                                                 int interval,
                                                 decimal tradeAmount,
                                                 decimal tilsonVolumeFactor,
                                                 int tilsonPeriod,
                                                 int bollingerPeriod,
                                                 decimal deviationMultiplier,
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