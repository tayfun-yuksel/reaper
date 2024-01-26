namespace Reaper.CommonLib.Interfaces;
public class Result<TResponse>()
{
    public TResponse? Data { get; set; }
    public Exception? Error { get; set; }
}