using System.IO;

namespace Common_Lib
{
    public class AppInfo
    {
        public readonly string DbConnection;
        public readonly TextFileHandler TxtFile;
        public readonly string Application;
        public readonly string Drive;
        public readonly string FilePath;
        public readonly string FileName;
        public readonly string FullPath;
        public readonly string ActiveDBConn;

        public readonly string ConfigFileName;
        public readonly string ConfigFullPath;
        public readonly TextFileHandler CnfFile;
        public readonly string DbProdConn;
        public readonly string DbTestConn;
        public readonly string DbAltConn;
        public readonly string TvmazeToken;
        public readonly int LogLevel;

        public AppInfo(string application, string dbconnection, string[] logfilepathelements)
        {
            Application = application;

            Common.EnvInfo envinfo = new();
            Drive = envinfo.Drive;
            if (logfilepathelements is not null)
            {
                FilePath = Drive;
                foreach (string element in logfilepathelements)
                {
                    FilePath = Path.Combine(FilePath, element);
                }
            }

            ConfigFileName = Application + ".cnf";
            ConfigFullPath = Path.Combine(FilePath, ConfigFileName);
            ReadKeyFromFile rkffo = new();
            LogLevel = int.Parse(rkffo.FindInArray(ConfigFullPath, "LogLevel"));

            FileName = Application + ".log";
            FullPath = Path.Combine(FilePath, FileName);

            TxtFile = new(FileName, Application, FilePath, LogLevel);
            CnfFile = new(ConfigFileName, Application, FilePath, LogLevel);

            DbProdConn= rkffo.FindInArray(ConfigFullPath, "DbProduction");
            DbTestConn = rkffo.FindInArray(ConfigFullPath, "DbTesting");
            DbAltConn = rkffo.FindInArray(ConfigFullPath, "DbAlternate");
            TvmazeToken = rkffo.FindInArray(ConfigFullPath, "TvmazeToken");

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
