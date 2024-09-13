using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Common_Lib;

public class AppInfo
{
    private readonly string _os = GetOperatingSystem();
    public readonly string HomeDir;
    public readonly bool IsDebugOn;
    public readonly string[] MediaExtensions;
    public readonly string Program;
    public readonly string TvmazeToken;
    public readonly TextFileHandler TxtFile;

    public AppInfo(string application, string program, string dbConnection)
    {
        Application = application;
        Program = program;
        HomeDir = _os switch {"Linux" => "/media/psf/TVMazeLinux", "macOS" => "/Users/dick/TVMazeLinux", _ => ""};

        ConfigFileName = Application + ".cnf";
        ConfigPath = HomeDir;
        ConfigFullPath = Path.Combine(HomeDir, ConfigFileName);
        if (!File.Exists(ConfigFullPath))
        {
            Console.WriteLine($"Config File Does not Exist {ConfigFullPath}");
            Environment.Exit(666);
        }

        var logLevel = int.Parse(ReadKeyFromFile.FindInArray(ConfigFullPath, "LogLevel"));
        var fileName = Program + ".log";
        var filePath = Path.Combine(HomeDir, "Logs");
        IsDebugOn = ReadKeyFromFile.FindInArray(ConfigFullPath, "Debug") == "Yes";
        TxtFile = new TextFileHandler(fileName, Program, filePath, logLevel);

        var dbProdConn = ReadKeyFromFile.FindInArray(ConfigFullPath, "DbProduction");
        var dbTestConn = ReadKeyFromFile.FindInArray(ConfigFullPath, "DbTesting");
        DbAltConn = ReadKeyFromFile.FindInArray(ConfigFullPath, "DbAlternate");
        TvmazeToken = ReadKeyFromFile.FindInArray(ConfigFullPath, "TvmazeToken");
        var me = ReadKeyFromFile.FindInArray(ConfigFullPath, "MediaExtensions");

        MediaExtensions = me.Split(", ");
        ActiveDbConn = dbConnection switch {"DbProduction" => dbProdConn, "DbTesting" => dbTestConn, "DbAlternate" => DbAltConn, _ => ""};
    }

    public string ActiveDbConn { get; }
    private string Application { get; }
    private string ConfigFileName { get; }
    private string ConfigFullPath { get; }
    public string? ConfigPath { get; }
    private string DbAltConn { get; }

    private static string GetOperatingSystem()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macOS" :
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "Linux" : "Unknown";
    }
}
