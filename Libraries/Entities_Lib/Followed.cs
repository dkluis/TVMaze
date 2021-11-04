using System;
using System.Collections.Generic;
using Common_Lib;
using DB_Lib;

namespace Entities_Lib
{
    public class Followed
    {
        private readonly TextFileHandler _log;
        private readonly MariaDb _mdb;
        public int IdInd = 0;

        public bool InDb;
        public int TvmShowId;
        public string UpdateDate = $"{DateTime.Now.Date:yyyy-MM-dd}";

        public Followed(AppInfo appInfo)
        {
            _mdb = new MariaDb(appInfo);
            _log = appInfo.TxtFile;
        }

        public void Reset()
        {
            TvmShowId = 0;
            UpdateDate = $"{DateTime.Now.Date:yyyy-MM-dd}";
            _mdb.Close();
        }

        public bool DbInsert(bool ignore = false)
        {
            var sql = $"insert into Followed values (0,'{TvmShowId}', '{UpdateDate}' );";
            var rows = _mdb.ExecNonQuery(sql, ignore);
            _mdb.Close();
            if (rows == 0) return false;
            _log.Write($"Followed {TvmShowId} is inserted", "", 4);
            return true;
        }

        public bool DbUpdate(bool ignore = false)
        {
            var updDate = DateTime.Now.ToString("yyyy-MM-dd");
            var sql = $"update Followed set `UpdateDate` = '{updDate}' where `TvmShowId` = {TvmShowId}";
            var rows = _mdb.ExecNonQuery(sql, ignore);
            _mdb.Close();
            if (rows == 0) return false;
            _log.Write($"Followed {TvmShowId} is updated", "", 4);
            return true;
        }

        public bool DbDelete(bool ignore = false)
        {
            var rows = _mdb.ExecNonQuery($"delete from Followed where `TvmShowId` = {TvmShowId}");
            _mdb.Close();
            if (rows == 0) return false;
            _log.Write($"Followed {TvmShowId} is deleted", "", 4);
            return true;
        }

        public bool DbDelete(int showId)
        {
            var rows = _mdb.ExecNonQuery($"delete from Followed where `TvmShowId` = {showId}");
            _mdb.Close();
            if (rows == 0) return false;
            _log.Write($"Followed {showId} is deleted", "", 4);
            return true;
        }

        public void GetFollowed(int showId)
        {
            var sql = $"select * from Followed where `TvmShowId` = {showId};";
            var rdr = _mdb.ExecQuery(sql);
            if (rdr.HasRows)
            {
                while (rdr.Read())
                {
                    TvmShowId = int.Parse(rdr["TvmShowId"].ToString()!);
                    UpdateDate = Convert.ToDateTime(rdr["UpdateDate"]).ToString("yyyy-MM-dd");
                    InDb = true;
                }
            }
            else
            {
                InDb = false;
                TvmShowId = showId;
                UpdateDate = Convert.ToDateTime(DateTime.Now).ToString("yyyy-MM-dd");
            }

            _mdb.Close();
        }

        public List<int> ShowsToDelete(List<int> followedOnTvmaze)
        {
            List<int> showsToDelete = new();
            List<int> followedInDb = new();

            using var rdr = _mdb.ExecQuery("select `TvmShowId` from Followed");
            while (rdr.Read()) followedInDb.Add(int.Parse(rdr["TvmShowId"].ToString()!));
            _mdb.Close();

            if (followedOnTvmaze.Count == followedInDb.Count) return showsToDelete;

            foreach (var showId in followedInDb)
            {
                if (followedOnTvmaze.Exists(e => e.Equals(showId))) continue;
                showsToDelete.Add(showId);
            }

            return showsToDelete;
        }
    }
}