using System;
using System.IO;

namespace Common_Lib
{
    public class AppInfo
    {
        public readonly string DbConnection;
        public readonly TextFileHandler TxtFile;
        public readonly string Application;
        public readonly string Program;
        public readonly string HomeDir;
        public readonly string Drive;
        public readonly string FilePath;
        public readonly string FileName;
        public readonly string FullPath;
        public readonly string ActiveDBConn;

        public readonly string ConfigFileName;
        public readonly string ConfigPath;
        public readonly string ConfigFullPath;
        public readonly TextFileHandler CnfFile;
        public readonly int LogLevel;

        public readonly string DbProdConn;
        public readonly string DbTestConn;
        public readonly string DbAltConn;

        public readonly string TvmazeToken;
        public readonly string RarbgToken;

        public readonly string[] MediaExtensions;

        public AppInfo(string application, string program, string dbconnection)
        {
            Application = application;
            Program = program;

            Common.EnvInfo envinfo = new();
            Drive = envinfo.Drive;
            if (envinfo.OS == "Windows")
            { HomeDir = Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%"); }
            else
            { HomeDir = Environment.GetEnvironmentVariable("HOME"); }
            HomeDir = Path.Combine(HomeDir, Application);

            ConfigFileName = Application + ".cnf";
            ConfigPath = HomeDir;
            ConfigFullPath = Path.Combine(HomeDir, ConfigFileName);
            if (!File.Exists(ConfigFullPath)) { Console.WriteLine($"Log File Does not Exist {ConfigFullPath}"); Environment.Exit(666); }
            ReadKeyFromFile rkffo = new();
            LogLevel = int.Parse(rkffo.FindInArray(ConfigFullPath, "LogLevel"));

            FileName = Program + ".log";
            FilePath = Path.Combine(HomeDir, "Logs");
            FullPath = Path.Combine(FilePath, FileName);

            TxtFile = new(FileName, Program, FilePath, LogLevel);
            CnfFile = new(ConfigFileName, Program, ConfigPath, LogLevel);

            DbProdConn = rkffo.FindInArray(ConfigFullPath, "DbProduction");
            DbTestConn = rkffo.FindInArray(ConfigFullPath, "DbTesting");
            DbAltConn = rkffo.FindInArray(ConfigFullPath, "DbAlternate");

            TvmazeToken = rkffo.FindInArray(ConfigFullPath, "TvmazeToken");
            RarbgToken = rkffo.FindInArray(ConfigFullPath, "RarbgToken");

            string ME = rkffo.FindInArray(ConfigFullPath, "MediaExtensions");
            MediaExtensions = ME.Split(", ");

            switch (dbconnection)
            {
                case "DbProduction":
                    ActiveDBConn = DbProdConn;
                    break;
                case "DbTesting":
                    ActiveDBConn = DbTestConn;
                    break;
                case "DbAlternate":
                    ActiveDBConn = DbAltConn;
                    break;
                default:
                    ActiveDBConn = "";
                    break;
            }
        }

    }
}
