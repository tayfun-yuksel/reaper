using System.Dynamic;
using Flurl.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Reaper.CommonLib.Interfaces;
using Reaper.Exchanges.Kucoin.Interfaces;
using Reaper.Exchanges.Kucoin.Services.Models;

namespace Reaper.Exchanges.Kucoin.Services;
public class PositionService(IOptions<KucoinOptions> options, 
    IMarketDataService marketDataService) : IPositionService
{
    private readonly KucoinOptions _kucoinOptions = options.Value;

    public async Task<decimal> GetPositionAmountAsync(string symbol, CancellationToken cancellationToken)
    {
        using var flurlClient = CommonLib.Utils.FlurlExtensions.GetFlurlClient(_kucoinOptions.FuturesBaseUrl, true);
        var getPositionFn = async (IFlurlClient client, object? requestData, CancellationToken cancellation) =>
            await client.Request()
                .AppendPathSegments("api", "v1", "position")
                .SetQueryParams(requestData)
                .WithSignatureHeaders(_kucoinOptions, "GET")
                .GetAsync(HttpCompletionOption.ResponseContentRead, cancellation)
                .ReceiveString();

        Result<string> result = await getPositionFn.CallAsync(flurlClient, new
        {
            symbol = symbol.ToUpper()
        }, cancellationToken);

        if (result.Error != null)
        {
            throw new InvalidOperationException("Error getting position details", result.Error);
        }
        dynamic positionDetails = JsonConvert.DeserializeObject<ExpandoObject>(result.Data!);
        var positionAmount = Math.Abs((decimal)positionDetails.data.markValue) + (decimal)positionDetails.data.realisedPnl;
        return positionAmount;
    }

}
