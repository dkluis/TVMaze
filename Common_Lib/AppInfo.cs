using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common_Lib
{
    public class AppInfo
    {
        public readonly string DbConnection;
        public readonly Logger Log;
        public readonly string Application;

        public AppInfo(string application, string dbconnection, string logfilename)
        {
            Application = application;
            DbConnection = dbconnection;
            Log = new(logfilename, Application);
        }

    }
}
