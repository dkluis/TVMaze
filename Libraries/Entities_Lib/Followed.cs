using System;
using System.Collections.Generic;
using Common_Lib;
using DB_Lib;

namespace Entities_Lib
{
    public class Followed
    {
        private AppInfo appInfo;
        public int Id;

        public bool inDB;
        private readonly TextFileHandler log;
        private readonly MariaDB Mdb;
        public int TvmShowId;
        public string UpdateDate = $"{DateTime.Now.Date:yyyy-MM-dd}";

        public Followed(AppInfo appinfo)
        {
            appInfo = appinfo;
            Mdb = new MariaDB(appinfo);
            log = appinfo.TxtFile;
        }

        public void Reset()
        {
            TvmShowId = 0;
            UpdateDate = $"{DateTime.Now.Date:yyyy-MM-dd}";
            Mdb.Close();
        }

        public bool DbInsert(bool ignore = false)
        {
            var values = "";
            var sqlpre = "insert into Followed values (";
            var sqlsuf = ");";

            values += $"{Id} ,";
            values += $"'{TvmShowId}', ";
            values += $"'{UpdateDate}' ";

            var rows = Mdb.ExecNonQuery(sqlpre + values + sqlsuf, ignore);
            Mdb.Close();
            if (rows == 0) return false;
            log.Write($"Followed {TvmShowId} is inserted", "", 4);
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

            var rows = Mdb.ExecNonQuery(sqlpre + updfields + sqlsuf, ignore);
            Mdb.Close();
            if (rows == 0) return false;
            log.Write($"Followed {TvmShowId} is updated", "", 4);
            return true;
        }

        public bool DbDelete(bool ignore = false)
        {
            var rows = Mdb.ExecNonQuery($"delete from Followed where `TvmShowId` = {TvmShowId}");
            Mdb.Close();
            if (rows == 0) return false;
            log.Write($"Followed {TvmShowId} is deleted", "", 4);
            return true;
        }

        public bool DbDelete(int showid)
        {
            var rows = Mdb.ExecNonQuery($"delete from Followed where `TvmShowId` = {showid}");
            Mdb.Close();
            if (rows == 0) return false;
            log.Write($"Followed {showid} is deleted", "", 4);
            return true;
        }

        public void GetFollowed(int showid)
        {
            var sql = $"select * from Followed where `TvmShowId` = {showid};";
            var rdr = Mdb.ExecQuery(sql);
            if (rdr.HasRows)
            {
                while (rdr.Read())
                {
                    Id = int.Parse(rdr["Id"].ToString());
                    TvmShowId = int.Parse(rdr["TvmShowId"].ToString());
                    UpdateDate = Convert.ToDateTime(rdr["UpdateDate"]).ToString("yyyy-MM-dd");
                    inDB = true;
                }
            }
            else
            {
                inDB = false;
                Id = 0;
                TvmShowId = showid;
                UpdateDate = Convert.ToDateTime(DateTime.Now).ToString("yyyy-MM-dd");
            }

            Mdb.Close();
        }

        public void SetUpdateDate(int ShowId, string UpdDate = "")
        {
            TvmShowId = ShowId;
            if (UpdDate != "")
                UpdateDate = UpdDate;
            else
                DateTime.Now.ToString("yyyy-MM-DD");
        }

        public List<int> ShowsToDelete(List<int> followedontvmaze)
        {
            List<int> showstodelete = new();
            List<int> followedindb = new();

            using (var rdr = Mdb.ExecQuery("select `TvmShowId` from Followed"))
            {
                while (rdr.Read()) followedindb.Add(int.Parse(rdr["TvmShowId"].ToString()));
                Mdb.Close();
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