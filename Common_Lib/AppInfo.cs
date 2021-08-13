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

        public readonly string ConfigFullPath;
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

            FileName = Application + ".log";
            FullPath = Path.Combine(FilePath, FileName);
            TxtFile = new(FileName, Application, FilePath);

            ConfigFullPath = Path.Combine(FilePath, Application + ".cnf");
            ReadKeyFromFile rkffo = new();
            DbProdConn= rkffo.FindInArray(ConfigFullPath, "DbProduction");
            DbTestConn = rkffo.FindInArray(ConfigFullPath, "DbTesting");
            DbAltConn = rkffo.FindInArray(ConfigFullPath, "DbAlternate");
            LogLevel = int.Parse(rkffo.FindInArray(ConfigFullPath, "LogLevel"));
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
