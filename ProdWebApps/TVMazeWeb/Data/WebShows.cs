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
            AppInfo appinfo = new("Tvmaze", "We", "DbAlternate");
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
            string sql = $"select * from Shows where `ShowName` like '{showname}' or `AltShowName` like '{showname}'";
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
