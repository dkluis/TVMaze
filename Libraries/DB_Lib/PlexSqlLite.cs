using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;

using Microsoft.Data.Sqlite;

using Common_Lib;

using DB_Lib_EF.Entities;

namespace DB_Lib;

public static class PlexSqlLite
{
    public static List<PlexWatchedInfo> PlexWatched(AppInfo appInfo)
    {
        var epochBase = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Local);;
        var epoch     = (DateTime.Now.Date.AddDays(-1) - epochBase).TotalSeconds;

        var plexPlayedItems = "select miv.grandparent_title, miv.parent_index, miv.`index`, miv.`viewed_at` from metadata_item_views miv " +
                              $"where miv.parent_index > 0 and miv.metadata_type = 4 and miv.`viewed_at` >= {epoch} "                     +
                              "and miv.account_id = 1 order by miv.`viewed_at` desc; ";

        List<PlexWatchedInfo> watchedEpisodes         = new();
        var                   connectionStringBuilder = new SqliteConnectionStringBuilder();
        connectionStringBuilder.DataSource = "/media/psf/TVMazeLinux/Plex/Plex.db";
        using var connection = new SqliteConnection(connectionStringBuilder.ConnectionString);
        connection.Open();
        var selectCommand = connection.CreateCommand();
        selectCommand.CommandText = plexPlayedItems;
        using var reader = selectCommand.ExecuteReader();

        if (reader is null) return watchedEpisodes;

        var showNames = "";

        while (reader.Read())
        {
            PlexWatchedInfo record = new();

            // var             field1 = reader[0].ToString()!;
            // var             field2 = reader[1].ToString()!;
            // var             field3 = reader[2].ToString()!;
            // var             field4 = reader[3].ToString()!;
            record.Fill(reader[0].ToString()!, int.Parse(reader[1].ToString()!), int.Parse(reader[2].ToString()!), int.Parse(reader[3].ToString()!));
            watchedEpisodes.Add(record);
            showNames += record.ShowName + "; ";
        }

        LogModel.Record("Update Plex Watched", "Sqlite", $"Found: {watchedEpisodes.Count} -> {showNames}", 1);

        return watchedEpisodes;
    }
}

public class PlexWatchedInfo
{
    private int     _episode        = 999999;
    private int     _season         = 999999;
    public  string  CleanedShowName = "";
    public  bool    ProcessedToTvmaze;
    public  string  SeasonEpisode = "";
    public  string  ShowName      = "";
    public  int     TvmEpisodeId;
    public  int     TvmShowId;
    public  string  UpdateDate  = " ";
    public  string? WatchedDate = "";

    public void Reset()
    {
        ShowName          = "";
        _season           = 999999;
        _episode          = 999999;
        SeasonEpisode     = "";
        WatchedDate       = "";
        ProcessedToTvmaze = false;
        UpdateDate        = " ";
        TvmShowId         = new int();
        TvmEpisodeId      = new int();
        CleanedShowName   = "";
    }

    public void Fill(string showName, int season, int episode, int watchedDate)
    {
        ShowName        = showName;
        _season         = season;
        _episode        = episode;
        WatchedDate     = Common.ConvertEpochToDate(watchedDate);
        UpdateDate      = DateTime.Now.ToString("yyyy-MM-dd");
        SeasonEpisode   = Common.BuildSeasonEpisodeString(season, episode);
        CleanedShowName = Common.RemoveSpecialCharsInShowName(showName);
    }

    public bool DbInsert(AppInfo appInfo)
    {
        var           success = false;
        using MariaDb mDbW    = new(appInfo);

        var sql = $"insert into `PlexWatchedEpisodes` values (0, {TvmShowId}, {TvmEpisodeId}, "  +
                  $"'{ShowName.Replace("'", "''")}', {_season}, {_episode}, '{SeasonEpisode}', " +
                  $"'{WatchedDate}', 0, '{DateTime.Now:yyyy-MM-dd}' );";
        var rows               = mDbW.ExecNonQuery(sql, true);
        if (rows == 1) success = true;

        return success;
    }

    public bool DbUpdate(AppInfo appInfo)
    {
        var           success  = false;
        using MariaDb mDbW     = new(appInfo);
        var           sql      = $"update `PlexWatchedEpisodes` set `ProcessedToTvmaze` = 1 where `TvmEpisodeId` = {TvmEpisodeId}";
        var           rows     = mDbW.ExecNonQuery(sql, true);
        if (rows == 1) success = true;

        return success;
    }
}
