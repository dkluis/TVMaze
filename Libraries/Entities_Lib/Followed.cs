using System;
using System.Collections.Generic;

using Common_Lib;
using DB_Lib;

using MySqlConnector;

namespace Entities_Lib
{
    public class Followed
    {
        public int Id = 0;
        public int TvmShowId = 0;
        public String UpdateDate = $"{DateTime.Now.Date:yyyy-MM-dd}";

        public bool inDB = false;

        AppInfo appInfo;
        MariaDB Mdb;
        TextFileHandler log;

        public Followed(AppInfo appinfo)
        {
            appInfo = appinfo;
            Mdb = new(appinfo);
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
            string values = "";
            string sqlpre = $"insert into Followed values (";
            string sqlsuf = $");";

            // values += $"'{}', ";  for strings
            // values += $"{}, ";    for ints
            values += $"{Id} ,";
            values += $"'{TvmShowId}', ";
            values += $"'{UpdateDate}' ";

            int rows = Mdb.ExecNonQuery(sqlpre + values + sqlsuf, ignore);
            Mdb.Close();
            if (rows == 0) { return false; }
            log.Write($"Followed {TvmShowId} is inserted", "", 4);
            return true;
        }

        public bool DbUpdate(bool ignore = false)
        {
            // updfields += $"`` = '{}', ";
            string updfields = "";
            string sqlpre = $"update Followed set ";
            updfields += $"`Id` = {Id}, ";
            updfields += $"`TvmShowId` = '{TvmShowId}', ";
            updfields += $"`UpdateDate` = '{DateTime.Now.ToString("yyyy-MM-dd")}' ";
            string sqlsuf = $"where `TvmShowId` = {TvmShowId};";
            int rows = Mdb.ExecNonQuery(sqlpre + updfields + sqlsuf, ignore);
            Mdb.Close();
            if (rows == 0) { return false;  }
            log.Write($"Followed {TvmShowId} is updated", "", 4);
            return true;
        }

        public bool DbDelete(bool ignore = false)
        {
            int rows = Mdb.ExecNonQuery($"delete from Followed where `TvmShowId` = {TvmShowId}");
            Mdb.Close();
            if (rows == 0) { return false; }
            log.Write($"Followed {TvmShowId} is deleted", "", 4);
            return true;
        }

        public bool DbDelete(int showid)
        {
            int rows = Mdb.ExecNonQuery($"delete from Followed where `TvmShowId` = {showid}");
            Mdb.Close();
            if (rows == 0) { return false; }
            log.Write($"Followed {showid} is deleted", "", 4);
            return true;
        }

        public void GetFollowed(int showid)
        {
            string sql = $"select * from Followed where `TvmShowId` = {showid};";
            MySqlDataReader rdr = Mdb.ExecQuery(sql);
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

        public void SetUpdateDate(int ShowId, String UpdDate = "")
        {
            TvmShowId = ShowId;
            if (UpdDate != "") { UpdateDate = UpdDate; } else { DateTime.Now.ToString("yyyy-MM-DD"); }
        }

        public List<int> ShowsToDelete(List<int> followedontvmaze)
        {
            List<int> showstodelete = new();
            List<int> followedindb = new();

            using (MySqlDataReader rdr = Mdb.ExecQuery($"select `TvmShowId` from Followed"))
            {
                while (rdr.Read())
                {
                    followedindb.Add(Int32.Parse(rdr["TvmShowId"].ToString()));
                }
                Mdb.Close();
            }

            if (followedontvmaze.Count == followedindb.Count) { return showstodelete; }

            foreach (int showid in followedindb)
            {
                if (followedontvmaze.Exists(e => e.Equals(showid))) { continue; }
                showstodelete.Add(showid);
            }

            return showstodelete;
        }

    }
}
