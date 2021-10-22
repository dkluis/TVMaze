using System;
using DB_Lib;
using Common_Lib;
using System.Collections.Generic;

namespace TVMazeWeb.Data
{
    public class WebShows
    {
        public WebShows()
        {
            AppInfo appinfo = new("Tvmaze", "WebUI", "DbAlternate");
        }

        public List<ShowsInfo> GetShowsByTvmStatus(AppInfo appinfo, string tvmstatus)
        {
            MariaDB mdbshows = new(appinfo);
            MySqlConnector.MySqlDataReader rdr;
            List<ShowsInfo> newShows = new();
            string sql = $"select * from Shows where `TvmStatus` = '{tvmstatus}'";
            rdr = mdbshows.ExecQuery(sql);
            while (rdr.Read())
            {
                ShowsInfo rec = new();

                rec.TvmShowId = int.Parse(rdr["TvmShowId"].ToString());
                rec.ShowName = rdr["ShowName"].ToString();
                rec.TvmStatus = rdr["TvmStatus"].ToString();
                rec.TvmUrl = rdr["TvmUrl"].ToString();
                rec.Finder = rdr["Finder"].ToString();
                rec.AltShowName = rdr["AltShowName"].ToString();
                rec.MediaType = rdr["MediaType"].ToString();
                rec.UpdateDate = rdr["UpdateDate"].ToString();

                newShows.Add(rec);
            }
            return newShows;
        }

        public List<ShowsInfo> FindShows(AppInfo appinfo, string showname)
        {
            MariaDB mdbshows = new(appinfo);
            MySqlConnector.MySqlDataReader rdr;
            List<ShowsInfo> newShows = new();
            string sql = $"select * from Shows where `ShowName` like '%{showname}%' or `AltShowName` like '%{showname}%' order by `TvmShowId` desc";
            rdr = mdbshows.ExecQuery(sql);
            while (rdr.Read())
            {
                ShowsInfo rec = new();

                rec.TvmShowId = int.Parse(rdr["TvmShowId"].ToString());
                rec.ShowName = rdr["ShowName"].ToString();
                rec.TvmStatus = rdr["TvmStatus"].ToString();
                rec.ShowStatus = rdr["ShowStatus"].ToString();
                rec.TvmUrl = rdr["TvmUrl"].ToString();
                rec.Finder = rdr["Finder"].ToString();
                rec.AltShowName = rdr["AltShowName"].ToString();
                rec.MediaType = rdr["MediaType"].ToString();
                rec.UpdateDate = rdr["UpdateDate"].ToString();

                newShows.Add(rec);
            }
            return newShows;
        }

        public bool DeleteShow(AppInfo appinfo, int showid)
        {
            MariaDB mdbshows = new(appinfo);
            string sql = $"delete from Shows where `TvmShowId` = {showid}";
            int resultrows = mdbshows.ExecNonQuery(sql);
            if (resultrows > 0) { return true; } else { return false; }
        }
    }

    public class ShowsInfo
    {
        public int TvmShowId;
        public string ShowName;
        public string TvmStatus;
        public string TvmUrl;
        public string Finder;
        public string ShowStatus;
        public string AltShowName;
        public string MediaType;
        public string UpdateDate;
    }
}
