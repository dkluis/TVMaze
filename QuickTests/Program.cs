using Common_Lib;

namespace QuickTests
{
    class Program
    {
        static void Main()
        {
            string[] appPath = new string[] { "Users", "Dick", "TVMaze" };
            AppInfo appinfo = new("QuickTest", "DbProduction", appPath);
            TextFileHandler log = appinfo.TxtFile;
            TextFileHandler cnf = appinfo.CnfFile;
            log.Start();
            
            log.Write($"{appinfo.DbProdConn}");
            log.Write($"{appinfo.ConfigFullPath}");
            log.Write($"{appinfo.TvmazeToken}");
            log.Write($"{appinfo.LogLevel}");
            log.Write($"{appinfo.ActiveDBConn}");


            ReadKeyFromFile rkffo = new();
            log.Write($"Direct Object Read: {rkffo.FindInArray(appinfo.ConfigFullPath, "LogLevel")}");
            log.Write($"Direct Object Read: {rkffo.FindInArray(appinfo.ConfigFullPath, "DbAlternate")}");
            log.Write($"Direct Object Read: {rkffo.FindInArray(appinfo.ConfigFullPath, "SSS")}");
            
            log.Stop();
        }
    }
}
