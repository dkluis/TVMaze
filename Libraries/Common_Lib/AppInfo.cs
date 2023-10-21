using System;
using System.IO;

namespace Common_Lib;

public class AppInfo
{
    //public readonly string DbConnection;
    public readonly string          DbProdConn;
    public readonly string          DbTestConn;
    public readonly string          Drive;
    public readonly string          FileName;
    public readonly string          FilePath;
    public readonly string          FullPath;
    public readonly string?         HomeDir;
    public readonly int             LogLevel;
    public readonly string[]        MediaExtensions;
    public readonly string          Program;
    public readonly string          Torrentz2Token;
    public readonly string          TvmazeToken;
    public readonly TextFileHandler TxtFile;
    public AppInfo(string application, string program, string dbConnection)
    {
        Application = application;
        Program     = program;

        Common.EnvInfo envInfo = new();
        Drive = envInfo.Drive;
        HomeDir = envInfo.Os == "Windows"
                      ? Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%")
                      : Environment.GetEnvironmentVariable("HOME");

        if (HomeDir is not null)
        {
            HomeDir = Path.Combine(HomeDir, Application);
        } else
        {
            Console.WriteLine("Could not determine HomeDir");
            Environment.Exit(666);
        }

        ConfigFileName = Application + ".cnf";
        ConfigPath     = HomeDir;
        ConfigFullPath = Path.Combine(HomeDir, ConfigFileName);
        if (!File.Exists(ConfigFullPath))
        {
            Console.WriteLine($"Log File Does not Exist {ConfigFullPath}");
            Environment.Exit(666);
        }

        ReadKeyFromFile readKeyFromFile = new();
        LogLevel = int.Parse(readKeyFromFile.FindInArray(ConfigFullPath, "LogLevel"));

        FileName = Program + ".log";
        FilePath = Path.Combine(HomeDir,  "Logs");
        FullPath = Path.Combine(FilePath, FileName);

        TxtFile = new TextFileHandler(FileName, Program, FilePath, LogLevel);
        //CnfFile = new TextFileHandler(ConfigFileName, Program, ConfigPath, LogLevel);

        DbProdConn = readKeyFromFile.FindInArray(ConfigFullPath, "DbProduction");
        DbTestConn = readKeyFromFile.FindInArray(ConfigFullPath, "DbTesting");
        DbAltConn  = readKeyFromFile.FindInArray(ConfigFullPath, "DbAlternate");

        TvmazeToken    = readKeyFromFile.FindInArray(ConfigFullPath, "TvmazeToken");
        Torrentz2Token = readKeyFromFile.FindInArray(ConfigFullPath, "Torrentz2Token");

        var me = readKeyFromFile.FindInArray(ConfigFullPath, "MediaExtensions");
        MediaExtensions = me.Split(", ");

        switch (dbConnection)
        {
            case "DbProduction":
                ActiveDbConn = DbProdConn;
                break;
            case "DbTesting":
                ActiveDbConn = DbTestConn;
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
