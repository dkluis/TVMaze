using Common_Lib;
using MySqlConnector;
using System;

namespace DB_Lib
{
    public class TvmSql : IDisposable
    {
        private readonly MariaDB db;
        MySqlDataReader rdr;

        public TvmSql(string conninfo, Logger logger)
        {
        db =new(conninfo, logger);
        }

        public Int32 GetLastTvmShowIdInserted()
        {
            Int32 LastShowInserted = 99999999;
            rdr = db.ExecQuery($"select TvmShowId from TvmShowUpdates order by TvmShowId desc limit 1;");
            while (rdr.Read())
            {
                LastShowInserted = Int32.Parse(rdr["TvmShowid"].ToString());
            }
            return LastShowInserted;
        }

        public bool IsShowIdFollowed(Int32 showid)
        {
            bool isFollowed = false;
            rdr = db.ExecQuery($"select TvmShowId from Shows where `TvmShowId` = {showid};");
            while (rdr.Read())
            {
                isFollowed = true;
            }
            return isFollowed;
        }

        public void Dispose()
        {
            db.Close();
            GC.SuppressFinalize(this);
        }
    }
}
