using System.Security.Cryptography;
using System.Text;
using Flurl.Http;
using Microsoft.Extensions.Configuration;

namespace Reaper.Exchanges.Services.Kucoin;
public static class FlurlExtensions
{
    private static string CreateSignature(string strToSign, string secretKey)
    {
        using var hmacsha256 = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey.Trim()));
        var hash = hmacsha256.ComputeHash(Encoding.UTF8.GetBytes(strToSign.Trim()));
        return Convert.ToBase64String(hash);
    }


    public static IFlurlClient GetFlurlClient() => new FlurlClient("https://api.kucoin.com/api/v1")
    {
        Settings = {
                Timeout = TimeSpan.FromMinutes(10)
            }
    };


    public static IFlurlRequest WithSignatureHeaders(this IFlurlRequest flurlRequest,
        IConfiguration configuration,
        string method,
        string body = "")
    {
        string apiKey = configuration["Kucoin:ApiKey"] ?? throw new InvalidOperationException(nameof(apiKey));
        string apiSecret = configuration["Kucoin:ApiSecret"] ?? throw new InvalidOperationException(nameof(apiSecret));
        string apiPassphrase = configuration["Kucoin:ApiPassphrase"] ?? throw new InvalidOperationException(nameof(apiPassphrase));

        var passphraseSignature = CreateSignature(apiPassphrase, apiSecret);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        var strForSign = timestamp + method + flurlRequest.Url.Path + body;
        var httpSignature = CreateSignature(strForSign, apiSecret);

        return flurlRequest
            .WithHeader("KC-API-KEY", apiKey)
            .WithHeader("KC-API-SIGN", httpSignature)
            .WithHeader("KC-API-TIMESTAMP", timestamp)
            .WithHeader("KC-API-PASSPHRASE", passphraseSignature)
            .WithHeader("KC-API-KEY-VERSION", "2");
    }

}