using System;

using Common_Lib;

namespace XXXXXXXXX
{
    public class XXXX : IDisposable
    {
        private readonly MariaDB Mdb;
        private readonly TextFileHandler log;
        private readonly AppInfo Appinfo;

        public XXXX(AppInfo appinfo)
        {
            Appinfo = appinfo;
            Mdb = new(appinfo);
            log = appinfo.TxtFile;
        }


        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}