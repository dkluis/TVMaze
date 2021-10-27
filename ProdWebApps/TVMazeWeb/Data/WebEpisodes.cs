using System.Collections.Generic;
using Common_Lib;
using DB_Lib;
using MySqlConnector;

namespace TVMazeWeb.Data
{
    public class WebEpisodes
    {
        public AppInfo Appinfo = new("Tvmaze", "WebUI", "DbAlternate");

        public List<EpisodeInfo> GetEpisodes(string showname, string season, string episode)
        {
            MariaDb mdbepisodes = new(Appinfo);
            MySqlDataReader rdr;
            List<EpisodeInfo> episodeinfolist = new();
            int seasonint;
            int episodeint;
            showname = showname.Replace("'", "''");
            var sql =
                $"select * from episodesfullinfo where (`ShowName` like '%{showname}%' or `AltShowName` like '%{showname}%')";
            if (int.TryParse(season, out seasonint)) sql = sql + $" and `Season` = {seasonint}";
            if (int.TryParse(episode, out episodeint)) sql = sql + $" and `Episode` = {episodeint}";
            sql = sql + " limit 150";

            rdr = mdbepisodes.ExecQuery(sql);
            while (rdr.Read())
            {
                EpisodeInfo ei = new();
                ei.TvmShowId = int.Parse(rdr["TvmShowId"].ToString());
                ei.ShowName = rdr["ShowName"].ToString();
                ei.Season = rdr["Season"].ToString();
                ei.Episode = rdr["Episode"].ToString();
                ei.BroadcastDate = rdr["BroadcastDate"].ToString();
                if (ei.BroadcastDate.Length > 10) ei.BroadcastDate = ei.BroadcastDate.Substring(0, 10);
                ei.TvmUrl = rdr["TvmUrl"].ToString();
                ei.PlexStatus = rdr["PlexStatus"].ToString();
                ei.PlexDate = rdr["PlexDate"].ToString();
                if (ei.PlexDate.Length > 10) ei.PlexDate = ei.PlexDate.Substring(0, 10);
                ei.UpdateDate = rdr["UpdateDate"].ToString().Substring(0, 10);

                episodeinfolist.Add(ei);
            }

            Appinfo.TxtFile.Write($"Executing in Episodes Page: {sql}: found {episodeinfolist.Count} records", "", 4);

            return episodeinfolist;
        }

        public List<EpisodeInfo> GetEpisodesToAcquire(bool includeshowrss)
        {
            MariaDb mdbacquire = new(Appinfo);
            MySqlDataReader rdr;
            List<EpisodeInfo> episodeinfolist = new();
            var sqlnoshowrss =
                "select * from episodesfromtodayback where finder = 'Multi' order by `BroadcastDate` asc, `ShowName` asc";
            var sqlwithshowrss = "select * from episodesfromtodayback order by `BroadcastDate` asc, `ShowName` asc";
            string sql;
            if (includeshowrss)
                sql = sqlwithshowrss;
            else
                sql = sqlnoshowrss;
            rdr = mdbacquire.ExecQuery(sql);
            while (rdr.Read())
            {
                EpisodeInfo ei = new();
                ei.TvmShowId = int.Parse(rdr["TvmShowId"].ToString());
                ei.ShowName = rdr["ShowName"].ToString();
                ei.Season = rdr["Season"].ToString();
                ei.Episode = rdr["Episode"].ToString();
                ei.BroadcastDate = rdr["BroadcastDate"].ToString();
                if (ei.BroadcastDate.Length > 10) ei.BroadcastDate = ei.BroadcastDate.Substring(0, 10);
                ei.TvmUrl = rdr["TvmUrl"].ToString();
                ei.PlexStatus = rdr["PlexStatus"].ToString();
                ei.PlexDate = "";
                ei.UpdateDate = rdr["UpdateDate"].ToString().Substring(0, 10);

                episodeinfolist.Add(ei);
            }

            return episodeinfolist;
        }
    }


    public class EpisodeInfo
    {
        public string BroadcastDate;
        public string Episode;
        public string PlexDate;
        public string PlexStatus;
        public string Season;
        public string ShowName;
        public int TvmShowId;
        public string TvmUrl;
        public string UpdateDate;
    }
}