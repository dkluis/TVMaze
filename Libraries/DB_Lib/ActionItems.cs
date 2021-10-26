using System;
using Common_Lib;

namespace DB_Lib
{
    public class ActionItems : IDisposable
    {
        private readonly AppInfo appinfo;
        private TextFileHandler log;
        private readonly MariaDB Mdbw;

        public ActionItems(AppInfo appin)
        {
            log = appin.TxtFile;
            Mdbw = new MariaDB(appin);
            appinfo = appin;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public bool DbInsert(string Message, bool ignore = false)
        {
            var success = false;
            int rows;
            rows = Mdbw.ExecNonQuery(
                $"insert into ActionItems values (0, '{appinfo.Program}', '{Message}', '{DateTime.Now.ToString("yyyy-MM-dd")}');",
                ignore);
            if (rows == 1) success = true;
            return success;
        }
    }
}