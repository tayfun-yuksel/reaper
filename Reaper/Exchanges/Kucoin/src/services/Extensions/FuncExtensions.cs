using Polly.Retry;
using Reaper.CommonLib.Interfaces;

namespace Reaper.Exchanges.Kucoin.Services;
public static class FuncExtensions
{

    public static async Task<T> WithPolicy<T>(this Func<Task<T>> fn, AsyncRetryPolicy policy)
    {
        return await policy.ExecuteAsync(fn);
    }

    public static async Task<Result<T>> WithErrorLogging<T>(this Task<T> fn)
    {
        try
        {
            var response = await fn;
            return new() { Data = response };
        }
        catch (OperationCanceledException oe)
        {
            Console.WriteLine(oe.Message);
            return new() { Error = oe };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return new() { Error = ex};
        }

    }

    public static async Task<Result<T>> WithDelayAndRetryAsync<T>(this Func<Task<T>> mainFn,
        Func<T, bool> handleResultFn,
        TimeSpan delay,
        TimeSpan limit)
    {
        Result<T> result = new();
        try
        {
            while (limit > TimeSpan.Zero)
            {
                T response = await mainFn();
                if (handleResultFn(response))
                {
                    result.Data = response;
                    return result;
                }
                limit -= TimeSpan.FromSeconds(delay.TotalSeconds);
                await Task.Delay(delay);
            }
        }
        catch (Exception ex)
        {
            result.Error = ex;
        }

        return result;
    }

}