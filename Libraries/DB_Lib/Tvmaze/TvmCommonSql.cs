using System;
using Common_Lib;
using MySqlConnector;

namespace DB_Lib
{
    public class TvmCommonSql : IDisposable
    {
        private readonly MariaDB db;
        private MySqlDataReader rdr;

        public TvmCommonSql(AppInfo appinfo)
        {
            db = new MariaDB(appinfo);
        }


        public void Dispose()
        {
            db.Close();
            GC.SuppressFinalize(this);
        }

        public int GetLastTvmShowIdInserted()
        {
            var LastShowInserted = 99999999;
            rdr = db.ExecQuery("select TvmShowId from TvmShowUpdates order by TvmShowId desc limit 1;");
            while (rdr.Read()) LastShowInserted = int.Parse(rdr["TvmShowid"].ToString());
            db.Close();
            return LastShowInserted;
        }

        public bool IsShowIdFollowed(int showid)
        {
            var isFollowed = false;
            rdr = db.ExecQuery($"select TvmShowId from Followed where `TvmShowId` = {showid};");
            while (rdr.Read()) isFollowed = true;
            db.Close();
            return isFollowed;
        }

        public bool IsShowIdEnded(int showid)
        {
            var isEnded = false;
            rdr = db.ExecQuery($"select ShowStatus from Shows where `TvmShowId` = {showid};");
            while (rdr.Read())
            {
                if (rdr["ShowStatus"].ToString() == "Ended") isEnded = true;
                ;
            }

            db.Close();
            return isEnded;
        }

        public int GetIdViaShowid(int showid)
        {
            var Id = 0;
            rdr = db.ExecQuery($"select Id from Shows where `TvmShowId` = {showid};");
            if (rdr is null) return 0;

            while (rdr.Read()) Id = int.Parse(rdr["Id"].ToString());
            return Id;
        }

        public int GetShowEpoch(int showid)
        {
            var epoch = 0;
            rdr = db.ExecQuery($"select `TvmUpdateEpoch` from TvmShowUpdates where `TvmShowId` = {showid};");
            if (rdr is null) return epoch;

            while (rdr.Read()) epoch = int.Parse(rdr["TvmUpdateEpoch"].ToString());

            return epoch;
        }

        public int GetLastEvaluatedShow()
        {
            var epoch = 0;

            rdr = db.ExecQuery("select `ShowId` from LastShowEvaluated where `Id` = 1;");
            if (rdr is null)
            {
                db.Close();
                return epoch;
            }

            while (rdr.Read()) epoch = int.Parse(rdr["ShowId"].ToString());
            db.Close();
            return epoch;
        }

        public void SetLastEvaluatedShow(int newlastepoch)
        {
            db.ExecNonQuery($"update LastShowEvaluated set `ShowId` = {newlastepoch};");
            db.Close();
        }
    }
}