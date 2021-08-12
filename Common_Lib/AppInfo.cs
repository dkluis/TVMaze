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
        public readonly string LoggerFilePath;
        public readonly string LoggerFileName;

        public AppInfo(string application, string dbconnection, string logfilename, string[] logfilepathelements = null)
        {
            Application = application;
            DbConnection = dbconnection;
            LoggerFileName = logfilename;

            Common.EnvInfo envinfo = new();
            Drive = envinfo.Drive;
            if (logfilepathelements is not null)
            {
                LoggerFilePath = Drive;
                foreach (string element in logfilepathelements)
                {
                    LoggerFilePath = Path.Combine(LoggerFilePath, element);
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
                LoggerFilePath = Path.Combine(logpath, logfilename);
            }

            Log = new(logfilename, Application);  // Adjust to starting using the LoggerFilePath
        }

    }
}
