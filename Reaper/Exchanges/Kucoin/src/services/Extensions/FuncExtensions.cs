using Polly.Retry;
using Reaper.CommonLib.Interfaces;

namespace Reaper.Exchanges.Kucoin.Services;
public static class FuncExtensions
{
    public static Func<Task<T>> WithPolicy<T>(this Func<Task<T>> fn, AsyncRetryPolicy policy)
    {
        return () => policy.ExecuteAsync(fn);
    }


    public static async Task<Result<T>> WrapErrorAsync<T>(this Func<Task<T>> fn)
    {
        try
        {
            var response = await fn();
            return new() { Data = response };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return new() { Error = ex};
        }

    }
}