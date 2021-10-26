using System;
using System.Collections.Generic;
using System.Data.SQLite;
using Common_Lib;
using MySqlConnector;

namespace DB_Lib
{
    public class PlexSqlLite
    {
        public List<PlexWatchedInfo> PlexWatched(AppInfo appinfo)
        {
            var PlexPlayedItems =
                "select miv.grandparent_title, miv.parent_index, miv.`index`, miv.`viewed_at` from metadata_item_views miv " +
                "where miv.parent_index > 0 and miv.metadata_type = 4 and (miv.viewed_at > date('now', '-1 day') and miv.viewed_at < datetime('now', '-4 hours', '-5 minutes')) and miv.account_id = 1 " +
                "order by miv.grandparent_title, miv.parent_index, miv.`index`; ";
            var PlexDBLocation =
                "Data Source=/Users/dick/Library/Application Support/Plex Media Server/Plug-in Support/Databases/com.plexapp.plugins.library.db";
            var log = appinfo.TxtFile;
            List<PlexWatchedInfo> watchedepisodes = new();
            MySqlConnection Mdbw = new(appinfo.ActiveDbConn);

            SQLiteConnection con = new(PlexDBLocation);
            con.Open();

            SQLiteCommand cmd = new(PlexPlayedItems, con);
            var rdr = cmd.ExecuteReader();

            if (rdr is not null)
                while (rdr.Read())
                {
                    PlexWatchedInfo record = new();
                    record.Fill(rdr[0].ToString(), int.Parse(rdr[1].ToString()), int.Parse(rdr[2].ToString()),
                        rdr[3].ToString());
                    watchedepisodes.Add(record);
                }

            return watchedepisodes;
        }
    }

    public class PlexWatchedInfo
    {
        public string CleanedShowName = "";
        public int Episode = 999999;
        public bool ProcessedToTvmaze;
        public int Season = 999999;
        public string SeasonEpisode = "";
        public string ShowName = "";
        public int TvmEpisodeId;

        public int TvmShowId;
        public string UpdateDate = " ";
        public string WatchedDate;

        public void Reset()
        {
            ShowName = "";
            Season = 999999;
            Episode = 999999;
            SeasonEpisode = "";
            WatchedDate = "";
            ProcessedToTvmaze = false;
            UpdateDate = " ";
            TvmShowId = new int();
            TvmEpisodeId = new int();
            CleanedShowName = "";
        }

        public void Fill(string showname, int season, int episode, string watcheddate)
        {
            ShowName = showname;
            Season = season;
            Episode = episode;
            var date = watcheddate.Split(" ")[0];
            var d = date.Split(@"/");
            date = d[2] + "-" + d[0].PadLeft(2, '0') + "-" + d[1].PadLeft(2, '0');
            WatchedDate = date;
            UpdateDate = DateTime.Now.ToString("yyyy-MM-dd");
            SeasonEpisode = Common.BuildSeasonEpisodeString(season, episode);
            CleanedShowName = Common.RemoveSpecialCharsInShowName(showname);
        }

        public bool DbInsert(AppInfo appinfo)
        {
            int rows;
            var success = false;
            using (MariaDB Mdbw = new(appinfo))
            {
                var sql = "insert into `PlexWatchedEpisodes` values (";
                sql += $"0, {TvmShowId}, {TvmEpisodeId}, ";
                sql += $"'{ShowName.Replace("'", "''")}', {Season}, {Episode}, ";
                sql += $"'{SeasonEpisode}', '{WatchedDate}', ";
                sql += $"0, '{DateTime.Now.ToString("yyyy-MM-dd")}' ); ";
                rows = Mdbw.ExecNonQuery(sql, true);
                if (rows == 1) success = true;
            }

            return success;
        }

        public bool DbUpdate(AppInfo appInfo)
        {
            var rows = 0;
            var success = false;
            using (MariaDB Mdbw = new(appInfo))
            {
                var sql =
                    $"update `PlexWatchedEpisodes` set `ProcessedToTvmaze` = 1 where `TvmEpisodeId` = {TvmEpisodeId}";
                rows = Mdbw.ExecNonQuery(sql, true);
            }

            if (rows == 1) success = true;
            return success;
        }
    }
}