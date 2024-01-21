
using NugetPacker;

Version version = new(1, 0, 5);
string nugetPath = "/Users/taf/localNugetStore";
string commonLibProjectPath = "../../CommonLib/src/";
string binancProjectPath = "../../Exchanges/Binance/src/";
string kucoinProjectPath = "../../Exchanges/Kucoin/src/";

Packer.Pack(commonLibProjectPath, nugetPath, version);
Dictionary<string, string> packagesToUpdate = Packer.GetLocalNugets(nugetPath);

void UpdateBinance() => Packer.GetCsProjFiles(binancProjectPath).UpdateWith(packagesToUpdate); 
void UpdateKucoin() => Packer.GetCsProjFiles(kucoinProjectPath).UpdateWith(packagesToUpdate); 
UpdateBinance();
UpdateKucoin();


