using Microsoft.Extensions.Configuration;

namespace Reaper.CommonLib.Utils;
public static class ConfigManager
{
    public static IConfiguration GetConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
        return configuration;
    }
}