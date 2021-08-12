using System.Collections.Generic;
using System.IO;

namespace Common_Lib
{
    public class AppInfo
    {
        public readonly string DbConnection;
        public readonly Logger Log;
        public readonly string Application;
        public readonly string Drive;
        public readonly string FilePath;
        public readonly string FileName;
        public readonly string FullPath;

        public AppInfo(string application, string dbconnection, string logfilename, string[] logfilepathelements = null)
        {
            Application = application;
            DbConnection = dbconnection;
            FileName = logfilename;

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
            else
            {
                string logpath;
                if (envinfo.OS == "Windows")
                {
                    logpath = Common.ReadConfig("PCLogPath");
                }
                else
                {
                    logpath = Common.ReadConfig("MacLogPath");
                }
                FilePath = Path.Combine(logpath, logfilename);
            }

            FullPath = Path.Combine(FilePath, FileName);

            Log = new(FileName, Application);  // Adjust to starting using the LoggerFilePath
        }

    }
}
