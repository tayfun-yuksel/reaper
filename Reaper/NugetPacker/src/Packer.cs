using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace NugetPacker;
public static class Packer
{
    private static Match VersionMatch(string str) => Regex.Match(str, @"(\d+\.\d+\.\d+)");

    internal static (string name, string version) ExtractPackageNameAndVersion(string filePath)
    {
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        var versionMatch = VersionMatch(fileName);

        if (versionMatch.Success)
        {
            return (fileName.Replace($".{versionMatch.Value}", string.Empty), versionMatch.Groups[1].Value);
        }

        throw new InvalidOperationException("Version not found");
    }



    internal static void CleanTarget(string srcPath, string targetPath)
    {
        try
        {
            DirectoryInfo di = new(targetPath);
            DirectoryInfo srcDir = new(srcPath);
            var oldCsProjFiles = srcDir.GetFiles("*.csproj", SearchOption.AllDirectories);
            var nugetPackages = di.GetFiles("*.nupkg", SearchOption.AllDirectories);
            nugetPackages.ToList()
                .ForEach(nugetPkg => oldCsProjFiles.ToList()
                .ForEach(csProj =>
                {
                    if (Path.GetFileNameWithoutExtension(csProj.Name)
                        .Contains(ExtractPackageNameAndVersion(nugetPkg.FullName).name))
                    {
                        nugetPkg.Delete();
                        Console.WriteLine($"deleted {nugetPkg.Name}");
                    }
                }));
    }
        catch (System.Exception)
        {
            throw;
        }
    }


    public static string[] GetCsProjFiles(string path)
{
    string[] csProjFiles = Directory.GetFiles(path, "*.csproj", SearchOption.AllDirectories);
    return csProjFiles;
}


public static void Pack(string srcPath, string nugetPath, Version version)
{
    CleanTarget(srcPath, nugetPath);
    string[] csProjFiles = Directory.GetFiles(srcPath, "*.csproj", SearchOption.AllDirectories);
    foreach (var csProjFile in csProjFiles)
    {
        string command = $"dotnet pack {csProjFile} -o {nugetPath} -p:Version={version}";
        Process process = new();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = $"/c {command}";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            process.StartInfo.FileName = "/bin/bash";
            process.StartInfo.Arguments = $"-c \"{command}\"";
        }
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.CreateNoWindow = true;
        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        Console.WriteLine(output);
        process.WaitForExit();
    }

    Console.WriteLine($"packing completed, updated packages");
    foreach (var item in csProjFiles)
    {
        Console.WriteLine(item);
    }
}



internal static void UpdatePackageReference(string csProjFile, Dictionary<string, string> packagesToUpdate)
{
    try
    {
        XDocument doc = XDocument.Load(csProjFile);
        var packageReferences = doc.Descendants("PackageReference");

        foreach (XElement packageReference in packageReferences)
        {
            XAttribute packageName = packageReference.Attribute("Include")!;
            Version srcVersion = new(VersionMatch(packageReference.Attribute("Version")!.Value).Value);
            if (!packagesToUpdate.TryGetValue(packageName!.Value, out var targetVersionStr))
            {
                continue;
            }
            Version targetVersion = new(targetVersionStr);
            if (srcVersion < targetVersion)
            {
                packageReference.Attribute("Version")!.Value = targetVersion.ToString();
            }
            else
            {
                Console.WriteLine($"Package {packageName} version({srcVersion}) is greater than {targetVersion} in {csProjFile}");
            }
        }
        doc.Save(csProjFile);
    }
    catch (System.Exception)
    {
        throw;
    }
}


public static void UpdateWith(this string[] csProjFiles, Dictionary<string, string> packagesToUpdate)
{
    foreach (var csProjFile in csProjFiles)
    {
        UpdatePackageReference(csProjFile, packagesToUpdate);
    }
}

public static Dictionary<string, string> GetLocalNugets(string path)
{
    var packages = Directory.GetFiles(path, "*.nupkg", SearchOption.AllDirectories)
        .Select(x => ExtractPackageNameAndVersion(x))
        .ToDictionary(x => x.name, x => x.version);
    return packages;
}
}