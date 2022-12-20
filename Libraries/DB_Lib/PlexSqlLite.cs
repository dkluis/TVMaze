using System;
using System.Collections.Generic;
using System.Data.SQLite;
using Common_Lib;

namespace DB_Lib;

public static class PlexSqlLite
{
    public static List<PlexWatchedInfo> PlexWatched(AppInfo appInfo)
    {
        var timeSpan = DateTime.Now.Subtract(DateTime.Now.TimeOfDay).AddDays(-1) - new DateTime(1970, 1, 1);
        var yesterday = (int) timeSpan.TotalSeconds + 60 * 60 * 4 + 1;
        var plexPlayedItems =
            "select miv.grandparent_title, miv.parent_index, miv.`index`, miv.`viewed_at` from metadata_item_views miv " +
            $"where miv.parent_index > 0 and miv.metadata_type = 4 and miv.viewed_at > {yesterday} " +
            "and miv.account_id = 1 order by miv.grandparent_title, miv.parent_index, miv.`index`; ";
        const string plexDbLocation =
            "Data Source=/Users/dick/Library/Application Support/Plex Media Server/Plug-in Support/Databases/com.plexapp.plugins.library.db";
        List<PlexWatchedInfo> watchedEpisodes = new();
        SQLiteConnection con = new(plexDbLocation);
        con.Open();

        SQLiteCommand cmd = new(plexPlayedItems, con);
        var rdr = cmd.ExecuteReader();
        if (rdr is null) return watchedEpisodes;
        while (rdr.Read())
        {
            PlexWatchedInfo record = new();
            record.Fill(rdr[0].ToString()!, int.Parse(rdr[1].ToString()!), int.Parse(rdr[2].ToString()!),
                int.Parse(rdr[3].ToString()!));
            watchedEpisodes.Add(record);
        }

        return watchedEpisodes;
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
    public string WatchedDate = "";

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

    public void Fill(string showName, int season, int episode, int watchedDate)
    {
        ShowName = showName;
        Season = season;
        Episode = episode;
        WatchedDate = Common.ConvertEpochToDate(watchedDate);
        UpdateDate = DateTime.Now.ToString("yyyy-MM-dd");
        SeasonEpisode = Common.BuildSeasonEpisodeString(season, episode);
        CleanedShowName = Common.RemoveSpecialCharsInShowName(showName);
    }

    public bool DbInsert(AppInfo appInfo)
    {
        var success = false;
        using MariaDb mDbW = new(appInfo);
        var sql =
            $"insert into `PlexWatchedEpisodes` values (0, {TvmShowId}, {TvmEpisodeId}, " +
            $"'{ShowName.Replace("'", "''")}', {Season}, {Episode}, '{SeasonEpisode}', " +
            $"'{WatchedDate}', 0, '{DateTime.Now:yyyy-MM-dd}' );";
        var rows = mDbW.ExecNonQuery(sql, true);
        if (rows == 1) success = true;
        return success;
    }

    public bool DbUpdate(AppInfo appInfo)
    {
        var success = false;
        using MariaDb mDbW = new(appInfo);
        var sql =
            $"update `PlexWatchedEpisodes` set `ProcessedToTvmaze` = 1 where `TvmEpisodeId` = {TvmEpisodeId}";
        var rows = mDbW.ExecNonQuery(sql, true);
        if (rows == 1) success = true;
        return success;
    }
}