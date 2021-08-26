using System;

using Common_Lib;

namespace DB_Lib
{
    public class ActionItems :IDisposable
    {
        TextFileHandler log;
        MariaDB Mdbw;
        AppInfo appinfo;

        public ActionItems(AppInfo appin)
        {
            log = appin.TxtFile;
            Mdbw = new(appin);
            appinfo = appin;
        }

        public bool DbInsert(string Message, bool ignore = false)
        {
            bool success = false;
            int rows;
            rows = Mdbw.ExecNonQuery($"insert into ActionItems values (0, '{appinfo.Program}', '{Message}', '{DateTime.Now.ToString("yyyy-MM-dd")}');", ignore);
            if (rows == 1) { success = true; }
            return success;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
