using Microsoft.AspNetCore.Mvc.Filters;

namespace Reaper.Exchanges.Kucoin.Api;
public class GlobalExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var exception = context.Exception;
        Services.RLogger.AppLog.Error(exception, "An error occurred");
        context.ExceptionHandled = true;
    }
}