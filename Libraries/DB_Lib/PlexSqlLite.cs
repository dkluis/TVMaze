using System;
using System.Collections.Generic;

using Microsoft.Data.Sqlite;
using System.Data.SQLite;
using System.Linq;

using Common_Lib;

using DB_Lib_EF.Entities;
using DB_Lib_EF.Models.MariaDB;

namespace DB_Lib;

public static class PlexSqlLite
{
    public static List<PlexWatchedInfo> PlexWatched(AppInfo appInfo)
    {
        var epochBase = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);;
        var epoch     = (DateTime.UtcNow.Date.AddDays(-2) - epochBase).TotalSeconds;

        var plexPlayedItems = "select miv.grandparent_title, miv.parent_index, miv.`index`, miv.`viewed_at` from metadata_item_views miv " +
                              $"where miv.parent_index > 0 and miv.metadata_type = 4 and miv.`viewed_at` >= {epoch} "                     +
                              "and miv.account_id = 1 order by miv.`viewed_at` ";

        var  watchedEpisodes = new List<PlexWatchedInfo>();
        var                   showNames       = "";

        using (var connection = new SQLiteConnection("Data Source=/media/psf/TVMazeLinux/Plex/Plex.db"))
        {
            connection.Open();

            using (var command = new SQLiteCommand(plexPlayedItems, connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var record = new PlexWatchedInfo();

                        // Commented out for the new Sqlite Setup in SqliteMacOs
                        //record.Fill(reader[0].ToString()!, int.Parse(reader[1].ToString()!), int.Parse(reader[2].ToString()!), int.Parse(reader[3].ToString()!));
                        record.Fill(reader[0].ToString()!, int.Parse(reader[1].ToString()!), int.Parse(reader[2].ToString()!), reader[3].ToString()!);
                        watchedEpisodes.Add(record);
                        showNames += record.ShowName + "; ";
                    }
                }
            }
        }

        LogModel.Record("Update Plex Watched", "Sqlite", $"Found: {watchedEpisodes.Count} -> {showNames}", 1);

        return watchedEpisodes;

        /*List<PlexWatchedInfo> watchedEpisodes         = new();
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
        */
    }
}

public class PlexWatchedInfo
{
    public int     Episode        = 999999;
    public int     Season         = 999999;
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
        Season           = 999999;
        Episode          = 999999;
        SeasonEpisode     = "";
        WatchedDate       = "";
        ProcessedToTvmaze = false;
        UpdateDate        = " ";
        TvmShowId         = new int();
        TvmEpisodeId      = new int();
        CleanedShowName   = "";
    }

    public void Fill(string showName, int season, int episode, string watchedDate)
    {
        ShowName        = showName;
        Season         = season;
        Episode        = episode;
        WatchedDate     = watchedDate;
        UpdateDate      = DateTime.Now.ToString("yyyy-MM-dd");
        SeasonEpisode   = Common.BuildSeasonEpisodeString(season, episode);
        CleanedShowName = Common.RemoveSpecialCharsInShowName(showName);
    }

    public bool DbInsert(AppInfo appInfo)
    {
        var success = false;
        using var db      = new TvMaze();
        var result  = db.PlexWatchedEpisodes.SingleOrDefault(p => p.TvmShowId == TvmShowId && p.TvmEpisodeId == TvmEpisodeId);

        if (result != null) return success;
        using MariaDb mDbW    = new(appInfo);

        var sql = $"insert into `PlexWatchedEpisodes` values (0, {TvmShowId}, {TvmEpisodeId}, "  +
                  $"'{ShowName.Replace("'", "''")}', {Season}, {Episode}, '{SeasonEpisode}', " +
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
