using Common_Lib;
using MySqlConnector;
using System;

namespace DB_Lib
{
    public class TvmCommonSql : IDisposable
    {
        private readonly MariaDB db;
        MySqlDataReader rdr;
        private readonly string connection;

        public TvmCommonSql(string conninfo, Logger logger)
        {
            db = new(conninfo, logger);
            connection = conninfo;
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

        public Int32 GetIdViaShowid(Int32 showid)
        {
            Int32 Id = 0;
            rdr = db.ExecQuery($"select Id from Shows where `TvmShowId` = {showid};");
            if (rdr is null)
            {
                return 0;
            }

            while (rdr.Read())
            {
                Id = Int32.Parse(rdr["Id"].ToString());
            }
            return Id;
        }

        public void Dispose()
        {
            db.Close();
            GC.SuppressFinalize(this);
        }
    }
}
