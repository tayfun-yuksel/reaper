using System.Runtime.InteropServices;
using NugetPacker;

Version version = new(1, 1, 253);
var getPath = (params string[] paths) =>
{
    DirectoryInfo currentDirectory = new(Directory.GetCurrentDirectory());

    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        for (int i = 0; i < 3; i++)
        {
            currentDirectory = currentDirectory.Parent!;
        }
    }
    foreach (var path in paths)
    {
        if (path.Equals(".."))
        {
            currentDirectory = currentDirectory.Parent!;
        }
        else
        {
            currentDirectory = new (Path.Combine(currentDirectory.FullName, path));
        }
    }

    return currentDirectory.FullName;
};



string nugetPath = getPath("..", "..", "localNugetStore");
string commonLibProjectPath = getPath("..", "..", "CommonLib", "src");
string signaleSentinelProjectPath = getPath("..", "..", "SignalSentinel", "src");
string binancProjectPath = getPath("..", "..", "Exchanges", "Binance", "src");
string kucoinProjectPath = getPath("..", "..", "Exchanges", "Kucoin", "src");

Packer.Pack(commonLibProjectPath, nugetPath, version);
Packer.Pack(signaleSentinelProjectPath, nugetPath, version);
Dictionary<string, string> localNugetStore = Packer.GetLocalNugets(nugetPath);

void UpdateBinance() => Packer.GetCsProjFiles(binancProjectPath).UpdateWith(localNugetStore); 
void UpdateKucoin() => Packer.GetCsProjFiles(kucoinProjectPath).UpdateWith(localNugetStore); 
UpdateBinance();
UpdateKucoin();


