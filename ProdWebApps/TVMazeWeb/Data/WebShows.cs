using System.Collections.Generic;
using Common_Lib;
using DB_Lib;
using MySqlConnector;

namespace TVMazeWeb.Data
{
    public class WebShows
    {
        public AppInfo Appinfo = new("Tvmaze", "WebUI", "DbAlternate");

        public List<ShowsInfo> GetShowsByTvmStatus(string tvmstatus)
        {
            MariaDb mdbshows = new(Appinfo);
            MySqlDataReader rdr;
            List<ShowsInfo> newShows = new();
            var sql = $"select * from Shows where `TvmStatus` = '{tvmstatus}' order by `TvmShowId` desc";
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

        public List<ShowsInfo> FindShows(string showname)
        {
            MariaDb mdbshows = new(Appinfo);
            MySqlDataReader rdr;
            List<ShowsInfo> newShows = new();
            if (showname is not null) showname = showname.Replace("'", "''");
            var sql =
                $"select * from Shows where `ShowName` like '%{showname}%' or `AltShowName` like '%{showname}%' order by `TvmShowId` desc limit 150";
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

        public bool DeleteShow(int showid)
        {
            MariaDb mdbshows = new(Appinfo);
            var sql = $"delete from Shows where `TvmShowId` = {showid}";
            var resultrows = mdbshows.ExecNonQuery(sql);
            if (resultrows > 0)
                return true;
            return false;
        }

        public bool SetTvmStatusShow(int showid, string newstatus)
        {
            MariaDb mdbshows = new(Appinfo);
            var sql = $"update shows set `TvmStatus` = '{newstatus}' where `TvmShowId` = {showid}";
            var resultrows = mdbshows.ExecNonQuery(sql);
            if (resultrows > 0)
                return true;
            return false;
        }

        public bool SetMtAndAsnShow(int showid, string mediatype, string altshowname)
        {
            MariaDb mdbshows = new(Appinfo);
            altshowname = altshowname.Replace("'", "''");
            var sql =
                $"update shows set `AltShowName` = '{altshowname}', `MediaType` = '{mediatype}' where `TvmShowId` = {showid}";
            var resultrows = mdbshows.ExecNonQuery(sql);
            if (resultrows > 0)
            {
                return true;
            }

            Appinfo.TxtFile.Write($"Edit Showname and MediaType unsuccesfull:  MediaType = {mediatype}");
            return false;
        }
    }

    public class ShowsInfo
    {
        public string AltShowName;
        public string Finder;
        public string MediaType;
        public string ShowName;
        public string ShowStatus;
        public int TvmShowId;
        public string TvmStatus;
        public string TvmUrl;
        public string UpdateDate;
    }
}