using System;
using Common_Lib;

namespace DB_Lib
{
    public class ActionItems : IDisposable
    {
        private readonly AppInfo _appInfo;
        private readonly MariaDb _mDbW;

        public ActionItems(AppInfo appInfo)
        {
            _mDbW = new MariaDb(appInfo);
            _appInfo = appInfo;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public bool DbInsert(string message, bool ignore = false)
        {
            var success = false;
            var rows = _mDbW.ExecNonQuery(
                $"insert into ActionItems values (0, '{_appInfo.Program}', '{message}', '{DateTime.Now:yyyy-MM-dd}');",
                ignore);
            if (rows == 1) success = true;
            return success;
        }
    }
}