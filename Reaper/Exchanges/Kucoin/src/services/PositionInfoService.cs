using System.Dynamic;
using Flurl.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Reaper.CommonLib.Interfaces;
using Reaper.Exchanges.Kucoin.Interfaces;
using Reaper.Exchanges.Kucoin.Services.Models;
using Reaper.SignalSentinel.Strategies;

namespace Reaper.Exchanges.Kucoin.Services;
public class PositionInfoService(IOptions<KucoinOptions> options) : IPositionInfoService
{
    private readonly KucoinOptions _kucoinOptions = options.Value;

    public async Task<Result<(decimal amount, decimal positionEnterPrice, SignalType position)>> GetPositionInfoAsync(string symbol, CancellationToken cancellationToken)
    {
        using var flurlClient = FlurlExtensions.GetHttpClient(_kucoinOptions);

        var getPositionFn = async () => await flurlClient.Request()
                .AppendPathSegments("api", "v1", "position")
                .SetQueryParams(new
                {
                    symbol = symbol.ToUpper()
                })
                .WithSignatureHeaders(_kucoinOptions, "GET")
                .GetAsync(HttpCompletionOption.ResponseContentRead, cancellationToken)
                .ReceiveString();

        Result<string> result = await getPositionFn
            .WithErrorPolicy(RetryPolicies.HttpErrorLogAndRetryPolicy)
            .CallAsync();

        if (result.Error != null)
        {
            return new() { Error = result.Error };
        }

        dynamic positionDetails = JsonConvert.DeserializeObject<ExpandoObject>(result.Data!);
        var positionAmount =  Math.Abs(Math.Floor((decimal)positionDetails.data.markValue))
                                + (decimal)positionDetails.data.realisedPnl;

        var markPrice = (decimal)positionDetails.data.markPrice;
        SignalType action = positionDetails.data.currentQty > 0 ? SignalType.Buy : SignalType.Sell;

        return new() { Data = (positionAmount, markPrice, action) };
    }

}
