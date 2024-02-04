using Polly.Retry;
using Reaper.CommonLib.Interfaces;

namespace Reaper.Exchanges.Kucoin.Services;
public static class FuncExtensions
{

    public static Func<Task> WithErrorPolicy(this Func<Task> fn, AsyncRetryPolicy policy)
    {
        return () => policy.ExecuteAsync(fn);
    }

    public static Func<Task<T>> WithErrorPolicy<T>(this Func<Task<T>> fn, AsyncRetryPolicy policy)
    {
        return () => policy.ExecuteAsync(fn);
    }


    public static Func<Task<T>> WithResponsePolicy<T>(this Func<Task<T>> fn, AsyncRetryPolicy<T> policy)
    {
        return () => policy.ExecuteAsync(fn);
    }

    public static async Task<Exception?> CallAsync(this Func<Task> fn)
    {
        try
        {
            await fn();
            return null;
        }
        catch (Exception ex)
        {
            RLogger.AppLog.Information(ex.Message);
            return ex;
        }
    }


    public static async Task<Result<T>> CallAsync<T>(this Func<Task<T>> fn)
    {
        try
        {
            var response = await fn();
            return new() { Data = response };
        }
        catch (Exception ex)
        {
            RLogger.AppLog.Information(ex.Message);
            return new() { Error = ex};
        }
    }
}