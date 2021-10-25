using System;
using DB_Lib;
using Common_Lib;
using System.Collections.Generic;

namespace TVMazeWeb.Data
{
    public class WebEpisodes
    {
        public AppInfo appinfo = new("Tvmaze", "WebUI", "DbAlternate");

        public List<EpisodeInfo> GetEpisodes(string showname, string season, string episode)
        {
            MariaDB mdbepisodes = new(appinfo);
            MySqlConnector.MySqlDataReader rdr;
            List<EpisodeInfo> episodeinfolist = new();
            int seasonint;
            int episodeint;
            showname = showname.Replace("'", "''");
            string sql = $"select * from episodesfullinfo where (`ShowName` like '%{showname}%' or `AltShowName` like '%{showname}%')";
            if (int.TryParse(season, out seasonint)) { sql = sql + $" and `Season` = {seasonint}"; }
            if (int.TryParse(episode, out episodeint)) { sql = sql + $" and `Episode` = {episodeint}"; }
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
                if (ei.BroadcastDate.Length > 10) { ei.BroadcastDate = ei.BroadcastDate.Substring(0, 10); }
                ei.TvmUrl = rdr["TvmUrl"].ToString();
                ei.PlexStatus = rdr["PlexStatus"].ToString();
                ei.PlexDate = rdr["PlexDate"].ToString();
                if (ei.PlexDate.Length > 10) { ei.PlexDate = ei.PlexDate.Substring(0, 10); }
                ei.UpdateDate = rdr["UpdateDate"].ToString().Substring(0, 10);

                episodeinfolist.Add(ei);
            }

            appinfo.TxtFile.Write($"Executing in Episodes Page: {sql}: found {episodeinfolist.Count} records", "", 4);
            
            return episodeinfolist;

        }

        public List<EpisodeInfo> GetEpisodesToAcquire(bool includeshowrss)
        {
            MariaDB mdbacquire = new(appinfo);
            MySqlConnector.MySqlDataReader rdr;
            List<EpisodeInfo> episodeinfolist = new();
            string sqlnoshowrss = $"select * from episodesfromtodayback where finder = 'Multi' order by `BroadcastDate` asc, `ShowName` asc";
            string sqlwithshowrss = $"select * from episodesfromtodayback order by `BroadcastDate` asc, `ShowName` asc";
            string sql;
            if (includeshowrss) { sql = sqlwithshowrss; } else { sql = sqlnoshowrss; }
            rdr = mdbacquire.ExecQuery(sql);
            while (rdr.Read())
            {
                EpisodeInfo ei = new();
                ei.TvmShowId = int.Parse(rdr["TvmShowId"].ToString());
                ei.ShowName = rdr["ShowName"].ToString();
                ei.Season = rdr["Season"].ToString();
                ei.Episode = rdr["Episode"].ToString();
                ei.BroadcastDate = rdr["BroadcastDate"].ToString();
                if (ei.BroadcastDate.Length > 10) { ei.BroadcastDate = ei.BroadcastDate.Substring(0, 10); }
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
        public int TvmShowId;
        public string ShowName;
        public string Season;
        public string Episode;
        public string BroadcastDate;
        public string TvmUrl;
        public string PlexStatus;
        public string PlexDate;
        public string UpdateDate;
    }
}
