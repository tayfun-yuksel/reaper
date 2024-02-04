using System.Dynamic;
using System.Net.WebSockets;
using Flurl.Http;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using Serilog;

namespace Reaper.Exchanges.Kucoin.Services;
public static class RetryPolicies
{
    public static AsyncRetryPolicy HttpErrorLogAndRetryPolicy => Policy
        .Handle<FlurlHttpException>()
        .WaitAndRetryAsync(
            3,
            retryAttempt => TimeSpan.FromSeconds(2),
            (exception, timeSpan, retryCount, context) => 
            {
                RLogger.AppLog.Error($"Retrying after {timeSpan.Seconds} seconds due to: {exception.Message}");
                RLogger.AppLog.Error($"RetryCount: {retryCount}");
            });


    public static AsyncRetryPolicy WebSocketLogAndRetryPolicy => Policy
        .Handle<WebSocketException>()
        .WaitAndRetryAsync(
            3,
            retryAttempt => TimeSpan.FromSeconds(2),
            (exception, timeSpan, retryCount, context) => 
            {
                RLogger.AppLog.Error($"Retrying after {timeSpan.Seconds} seconds due to: {exception.Message}");
                RLogger.AppLog.Error($"RetryCount: {retryCount}");
            });

    public static AsyncRetryPolicy<string> GetErrorMessageHandlePolicy(string errorMsg) => Policy
        .HandleResult<string>(responseStr =>
        {
            return responseStr.Contains(errorMsg);
        })
        .WaitAndRetryAsync(
            3,
            retryAttempt => TimeSpan.FromSeconds(2),
            onRetry: (outCome, timeSpan, retryCount, context) => 
            {
                RLogger.AppLog.Error($"Retrying after {timeSpan.Seconds} seconds due to: {errorMsg}", timeSpan.Seconds, errorMsg);
                RLogger.AppLog.Error("RetryCount: {retryCount}", retryCount);
            });

}
