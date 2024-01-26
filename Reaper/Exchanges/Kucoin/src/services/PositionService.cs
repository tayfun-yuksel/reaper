using Flurl.Http;
using Microsoft.Extensions.Options;
using Reaper.CommonLib.Interfaces;
using Reaper.Exchanges.Kucoin.Services.Models;

namespace Reaper.Exchanges.Kucoin.Services;
public class PositionService(IOptions<KucoinOptions> options) : IPositionService
{
    private readonly KucoinOptions _kucoinOptions = options.Value;

    public async Task<string> GetPositionDetailsAsync(string symbol, CancellationToken cancellationToken)
    {
        using var flurlClient = CommonLib.Utils.FlurlExtensions.GetFlurlClient(_kucoinOptions.FuturesBaseUrl, true);
        var closePositionFn = async (IFlurlClient client, object? requestData, CancellationToken cancellation) =>
            await client.Request()
                .AppendPathSegments("api", "v1", "position")
                .SetQueryParams(requestData)
                .WithSignatureHeaders(_kucoinOptions, "GET")
                .GetAsync(HttpCompletionOption.ResponseContentRead, cancellation)
                .ReceiveString();

        Result<string> result = await closePositionFn.CallAsync(flurlClient, new
        {
            symbol = symbol.ToUpper()
        }, cancellationToken);

        if (result.Error != null)
        {
            return result.Error.Message;
        }
        return result.Data!;
    }

}

public interface IPositionService
{
    Task<string> GetPositionDetailsAsync(string symbol, CancellationToken cancellationToken);
}