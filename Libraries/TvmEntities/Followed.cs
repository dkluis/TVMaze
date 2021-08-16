using System;
using System.Collections.Generic;

using Common_Lib;
using DB_Lib;

using MySqlConnector;

namespace TvmEntities
{
    public class Followed
    {
        private readonly Int32 Id = 0;
        public Int32 TvmShowId = 0;
        public String UpdateDate = $"{DateTime.Now.Date:yyyy-MM-dd}";

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
            return true;
        }

        public bool DbUpdate(bool ignore = false)
        {
            // updfields += $"`` = '{}', ";
            string updfields = "";
            string sqlpre = $"update Followed set ";
            updfields += $"`Id` = {Id}, ";
            updfields += $"`TvmShowId` = '{TvmShowId}', ";
            updfields += $"`UpdateDate` = '{UpdateDate}' ";
            string sqlsuf = $"where `TvmShowId` = {TvmShowId};";
            int rows = Mdb.ExecNonQuery(sqlpre + updfields + sqlsuf, ignore);
            Mdb.Close();
            if (rows == 0) { return false;  }
            return true;
        }

        public bool DbDelete(bool ignore = false)
        {
            int rows = Mdb.ExecNonQuery($"delete from Followed where `TvmShowId` = {TvmShowId}");
            log.Write($"DbDelete for Show: {TvmShowId}", "", 4);
            Mdb.Close();
            if (rows == 0) { return false; }
            return true;
        }

        public void Fill(Int32 ShowId, String UpdDate = "")
        {
            TvmShowId = ShowId;
            if (UpdDate != "") { UpdateDate = UpdDate; } 
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
