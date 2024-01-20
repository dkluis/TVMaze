using System.Data.SQLite;

namespace GetWatchedEpisodesToUbuntu;

public static class SqlLiteMacOs
{
    public static List<WatchedInfo> PlexWatched()
    {
        var epochBase = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var epoch     = (DateTime.UtcNow.Date.AddDays(-1) - epochBase).TotalSeconds;

        var plexPlayedItems = "select miv.grandparent_title, miv.parent_index, miv.`index`, miv.`viewed_at` from metadata_item_views miv " +
                              $"where miv.parent_index > 0 and miv.metadata_type = 4 and miv.`viewed_at` >= {epoch} "                      +
                              "and miv.account_id = 1 order by miv.`viewed_at` ";

        var watchedInfo = new List<WatchedInfo>();

        using (var connection = new SQLiteConnection("Data Source=/Users/dick/Library/Application Support/Plex Media Server/Plug-in Support/Databases/com.plexapp.plugins.library.db"))
        {
            connection.Open();

            using (var command = new SQLiteCommand(plexPlayedItems, connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var record = new PlexWatchedInfo();
                        record.Fill(reader[0].ToString()!, int.Parse(reader[1].ToString()!), int.Parse(reader[2].ToString()!), int.Parse(reader[3].ToString()!));

                        watchedInfo.Add(new WatchedInfo
                                        {
                                            CleanedShowName   = record.CleanedShowName,
                                            ProcessedToTvmaze = record.ProcessedToTvmaze,
                                            SeasonEpisode     = record.SeasonEpisode,
                                            ShowName          = record.ShowName,
                                            TvmEpisodeId      = record.TvmEpisodeId,
                                            TvmShowId         = record.TvmShowId,
                                            UpdateDate        = record.UpdateDate,
                                            WatchedDate       = record.WatchedDate,
                                            Season            = record.Season,
                                            Episode           = record.Episode,
                                        });
                    }
                }
            }
        }

        //Logging.Record("Update Plex Watched", "Sqlite", $"Found: {watchedEpisodes.Count} -> {showNames}", 1);

        return watchedInfo;
    }
}

public class WatchedInfo
{
    public string  CleanedShowName = "";
    public bool    ProcessedToTvmaze;
    public string  SeasonEpisode = "";
    public string  ShowName      = "";
    public int     TvmEpisodeId;
    public int     TvmShowId;
    public string  UpdateDate  = " ";
    public string? WatchedDate = "";
    public int     Episode;
    public int     Season;
}

public class PlexWatchedInfo
{
    public int     Episode         = 999999;
    public int     Season          = 999999;
    public string  CleanedShowName = "";
    public bool    ProcessedToTvmaze;
    public string  SeasonEpisode = "";
    public string  ShowName      = "";
    public int     TvmEpisodeId;
    public int     TvmShowId;
    public string  UpdateDate  = " ";
    public string? WatchedDate = "";

    public void Fill(string showName, int season, int episode, int watchedDate)
    {
        ShowName        = showName;
        Season          = season;
        Episode         = episode;
        WatchedDate     = Common.ConvertEpochToDate(watchedDate);
        UpdateDate      = DateTime.Now.ToString("yyyy-MM-dd");
        SeasonEpisode   = Common.BuildSeasonEpisodeString(season, episode);
        CleanedShowName = Common.RemoveSpecialCharsInShowName(showName);
    }
}

public class Common
{
    public static string ConvertEpochToDate(int epoch)
    {
        DateTime datetime = new(1970, 1, 1, 0, 0, 0);
        datetime = datetime.AddSeconds(epoch);
        var date = datetime.ToString("yyyy-MM-dd");

        return date;
    }

    public static string BuildSeasonEpisodeString(int seasNum, int epiNum)
    {
        return "s" + seasNum.ToString().PadLeft(2, '0') + "e" + epiNum.ToString().PadLeft(2, '0');
    }

    public static string RemoveSpecialCharsInShowName(string showName)
    {
        showName = showName.Replace("...", "")
                           .Replace("..",     "")
                           .Replace(".",      " ")
                           .Replace(",",      "")
                           .Replace("'",      "")
                           .Replace("   ",    " ")
                           .Replace("  ",     " ")
                           .Replace("'",      "")
                           .Replace("\"",     "")
                           .Replace("/",      "")
                           .Replace(":",      "")
                           .Replace("?",      "")
                           .Replace("|",      "")
                           .Replace("&#039;", "")
                           .Replace("&amp;",  "and")
                           .Replace("&",      "and")
                           .Replace("°",      "")
                           .Replace("\u2026", "")
                           .Trim()
                           .ToLower();

        // Was put in for the What If...? situation: showName = showName.Substring(0, showName.Length);
        if (showName.Length <= 7) return showName;

        if (showName.ToLower()[..7] == "what if")
            showName = "What If";

        return showName;
    }
}
