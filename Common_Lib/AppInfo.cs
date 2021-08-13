using System.Collections.Generic;
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

        public AppInfo(string application, string dbconnection, string logfilename, string[] logfilepathelements)
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

            FullPath = Path.Combine(FilePath, FileName);
            TxtFile = new(FileName, Application, FilePath); 
        }

    }
}
