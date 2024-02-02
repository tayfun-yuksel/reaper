using System.Net.WebSockets;
using Flurl.Http;
using Polly;
using Polly.Retry;

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
                Console.WriteLine($"Retrying after {timeSpan.Seconds}");
                Console.WriteLine($"seconds due to: {exception.Message}");
                Console.WriteLine($"RetryCount: {retryCount}");
            });


    public static AsyncRetryPolicy WebSocketLogAndRetryPolicy => Policy
        .Handle<WebSocketException>()
        .WaitAndRetryAsync(
            3,
            retryAttempt => TimeSpan.FromSeconds(2),
            (exception, timeSpan, retryCount, context) => 
            {
                Console.WriteLine($"Retrying after {timeSpan.Seconds}");
                Console.WriteLine($"seconds due to: {exception.Message}");
                Console.WriteLine($"RetryCount: {retryCount}");
            });
}