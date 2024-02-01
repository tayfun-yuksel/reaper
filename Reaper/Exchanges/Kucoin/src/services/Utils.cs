
namespace Reaper.Exchanges.Kucoin.Services;
public static class Utils
{
    public static void Print(string title, string message, ConsoleColor color = ConsoleColor.Green)
    {
        Console.ForegroundColor = color;
        Console.WriteLine($"========================={title}========================"); 
        Console.ResetColor();
        Console.WriteLine(message);
    }
}