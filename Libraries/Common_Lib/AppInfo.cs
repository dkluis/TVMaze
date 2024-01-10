using System;
using System.IO;

namespace Common_Lib;

public class AppInfo
{
    //public readonly string DbConnection;
    public readonly string          Drive;
    public readonly string          FullPath;
    public readonly string          HomeDir;
    public readonly string[]        MediaExtensions;
    public readonly string          Program;
    public readonly string          Torrentz2Token;
    public readonly string          TvmazeToken;
    public readonly TextFileHandler TxtFile;
    public readonly bool            IsDebugOn;

    public AppInfo(string application, string program, string dbConnection)
    {
        Application = application;
        Program     = program;

        Common.EnvInfo envInfo = new();
        Drive = envInfo.Drive;

        // HomeDir = envInfo.Os == "Windows" ? Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%") : Environment.GetEnvironmentVariable("HOME");
        //
        // if (HomeDir is not null)
        // {
        //     HomeDir = Path.Combine(HomeDir, Application);
        // } else
        // {
        //     Console.WriteLine("Could not determine HomeDir");
        //     Environment.Exit(666);
        // }

        HomeDir = "/media/psf/TVMazeLinux";

        ConfigFileName = Application + ".cnf";
        ConfigPath     = HomeDir;
        ConfigFullPath = Path.Combine(HomeDir, ConfigFileName);

        if (!File.Exists(ConfigFullPath))
        {
            Console.WriteLine($"Log File Does not Exist {ConfigFullPath}");
            Environment.Exit(666);
        }

        ReadKeyFromFile readKeyFromFile = new();
        var             logLevel        = int.Parse(ReadKeyFromFile.FindInArray(ConfigFullPath, "LogLevel"));

        var fileName = Program + ".log";
        var filePath = Path.Combine(HomeDir, "Logs");
        FullPath = Path.Combine(filePath, fileName);

        TxtFile = new TextFileHandler(fileName, Program, filePath, logLevel);

        if (ReadKeyFromFile.FindInArray(ConfigFullPath, "Debug") == "Yes") IsDebugOn = true;
        else IsDebugOn                                                               = false;

        var dbProdConn                                                               = ReadKeyFromFile.FindInArray(ConfigFullPath, "DbProduction");
        var dbTestConn                                                               = ReadKeyFromFile.FindInArray(ConfigFullPath, "DbTesting");
        DbAltConn = ReadKeyFromFile.FindInArray(ConfigFullPath, "DbAlternate");

        TvmazeToken    = ReadKeyFromFile.FindInArray(ConfigFullPath, "TvmazeToken");
        Torrentz2Token = ReadKeyFromFile.FindInArray(ConfigFullPath, "Torrentz2Token");

        var me = ReadKeyFromFile.FindInArray(ConfigFullPath, "MediaExtensions");
        MediaExtensions = me.Split(", ");

        switch (dbConnection)
        {
            case "DbProduction":
                ActiveDbConn = dbProdConn;

                break;

            case "DbTesting":
                ActiveDbConn = dbTestConn;

                break;

            case "DbAlternate":
                ActiveDbConn = DbAltConn;

                break;

            default:
                ActiveDbConn = "";

                break;
        }
    }

    public  string  ActiveDbConn   { get; }
    private string  Application    { get; }
    private string  ConfigFileName { get; }
    private string  ConfigFullPath { get; }
    public  string? ConfigPath     { get; }
    private string  DbAltConn      { get; }
}
