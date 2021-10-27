using System;
using System.Collections.Generic;
using Common_Lib;
using DB_Lib;

namespace Entities_Lib
{
    public class Followed
    {
        private AppInfo _appInfo;
        public int Id;

        public bool InDb;
        private readonly TextFileHandler _log;
        private readonly MariaDb _mdb;
        public int TvmShowId;
        public string UpdateDate = $"{DateTime.Now.Date:yyyy-MM-dd}";

        public Followed(AppInfo appinfo)
        {
            _appInfo = appinfo;
            _mdb = new MariaDb(appinfo);
            _log = appinfo.TxtFile;
        }

        public void Reset()
        {
            TvmShowId = 0;
            UpdateDate = $"{DateTime.Now.Date:yyyy-MM-dd}";
            _mdb.Close();
        }

        public bool DbInsert(bool ignore = false)
        {
            var values = "";
            var sqlpre = "insert into Followed values (";
            var sqlsuf = ");";

            values += $"{Id} ,";
            values += $"'{TvmShowId}', ";
            values += $"'{UpdateDate}' ";

            var rows = _mdb.ExecNonQuery(sqlpre + values + sqlsuf, ignore);
            _mdb.Close();
            if (rows == 0) return false;
            _log.Write($"Followed {TvmShowId} is inserted", "", 4);
            return true;
        }

        public bool DbUpdate(bool ignore = false)
        {
            var updfields = "";
            var sqlpre = "update Followed set ";

            updfields += $"`Id` = {Id}, ";
            updfields += $"`TvmShowId` = '{TvmShowId}', ";
            updfields += $"`UpdateDate` = '{DateTime.Now.ToString("yyyy-MM-dd")}' ";
            var sqlsuf = $"where `TvmShowId` = {TvmShowId};";

            var rows = _mdb.ExecNonQuery(sqlpre + updfields + sqlsuf, ignore);
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

        public bool DbDelete(int showid)
        {
            var rows = _mdb.ExecNonQuery($"delete from Followed where `TvmShowId` = {showid}");
            _mdb.Close();
            if (rows == 0) return false;
            _log.Write($"Followed {showid} is deleted", "", 4);
            return true;
        }

        public void GetFollowed(int showid)
        {
            var sql = $"select * from Followed where `TvmShowId` = {showid};";
            var rdr = _mdb.ExecQuery(sql);
            if (rdr.HasRows)
            {
                while (rdr.Read())
                {
                    Id = int.Parse(rdr["Id"].ToString());
                    TvmShowId = int.Parse(rdr["TvmShowId"].ToString());
                    UpdateDate = Convert.ToDateTime(rdr["UpdateDate"]).ToString("yyyy-MM-dd");
                    InDb = true;
                }
            }
            else
            {
                InDb = false;
                Id = 0;
                TvmShowId = showid;
                UpdateDate = Convert.ToDateTime(DateTime.Now).ToString("yyyy-MM-dd");
            }

            _mdb.Close();
        }

        public void SetUpdateDate(int showId, string updDate = "")
        {
            TvmShowId = showId;
            if (updDate != "")
                UpdateDate = updDate;
            else
                DateTime.Now.ToString("yyyy-MM-DD");
        }

        public List<int> ShowsToDelete(List<int> followedontvmaze)
        {
            List<int> showstodelete = new();
            List<int> followedindb = new();

            using (var rdr = _mdb.ExecQuery("select `TvmShowId` from Followed"))
            {
                while (rdr.Read()) followedindb.Add(int.Parse(rdr["TvmShowId"].ToString()));
                _mdb.Close();
            }

            if (followedontvmaze.Count == followedindb.Count) return showstodelete;

            foreach (var showid in followedindb)
            {
                if (followedontvmaze.Exists(e => e.Equals(showid))) continue;
                showstodelete.Add(showid);
            }

            return showstodelete;
        }
    }
}