using System.Collections.Generic;
using System.Data.SQLite;
using System;

using MySqlConnector;

using Common_Lib;

namespace DB_Lib
{
    public class PlexSqlLite
    {
        public List<PlexWatchedInfo> PlexWatched(AppInfo appinfo)
        {
            string PlexPlayedItems = "select miv.grandparent_title, miv.parent_index, miv.`index`, miv.`viewed_at` from metadata_item_views miv " +
                "where miv.parent_index > 0 and miv.metadata_type = 4 and miv.viewed_at > date('now', '-1 day') " +
                "order by miv.grandparent_title, miv.parent_index, miv.`index`; ";
            string PlexDBLocation = "Data Source=/Users/dick/Library/Application Support/Plex Media Server/Plug-in Support/Databases/com.plexapp.plugins.library.db";
            TextFileHandler log = appinfo.TxtFile;
            List<PlexWatchedInfo> watchedepisodes = new();
            MySqlConnection Mdbw = new(appinfo.ActiveDBConn);

            SQLiteConnection con = new(PlexDBLocation);
            con.Open();
           
            SQLiteCommand cmd = new(PlexPlayedItems, con);
            SQLiteDataReader rdr = cmd.ExecuteReader();

            if (rdr is not null)
            {
                while (rdr.Read())
                {
                    PlexWatchedInfo record = new();
                    record.Fill(rdr[0].ToString(), int.Parse(rdr[1].ToString()), int.Parse(rdr[2].ToString()), rdr[3].ToString());
                    watchedepisodes.Add(record);
                }
            }
            
            return watchedepisodes;
        }
    }

    public class PlexWatchedInfo
    {
        public string ShowName = "";
        public int Season = 999999;
        public int Episode = 999999;
        public string WatchedDate;
        public bool ProcessedToTvmaze;
        public string UpdateDate = " ";

        public int TvmShowId;
        public int TvmEpisodeId;

        public void Reset()
        {
            ShowName = "";
            Season = 999999;
            Episode = 999999;
            WatchedDate = "";
            ProcessedToTvmaze = false;
            UpdateDate = " ";
            TvmShowId = new();
            TvmEpisodeId = new();
        }

        public void Fill(string showname, int season, int episode, string watcheddate)
        {
            ShowName = showname;
            Season = season;
            Episode = episode;
            string date = watcheddate.Split(" ")[0];
            WatchedDate = date;
            UpdateDate = DateTime.Now.ToString("yyyy-MM-dd");
          
        }

        public bool UpdateTvmaze()
        {
            ProcessedToTvmaze = true;

            DbInsert();
            return false;
        }

        public bool DbInsert()
        {
            return false;
        }
    }
}
 