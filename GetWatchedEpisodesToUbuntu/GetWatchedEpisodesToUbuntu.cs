using System.Text.Json;
using System.IO;

using DB_Lib_EF.Models.MariaDB;

namespace GetWatchedEpisodesToUbuntu;

internal static class UpdatePlexWatchedMacOs
{
    private static void Main()
    {
        const string thisProgram = "Update Watched MacOs";

        var    watchedEpisodes = SqlLiteMacOs.PlexWatched();
        string json;
        var    list = new List<string>();

        foreach (var episode in watchedEpisodes)
        {
            var info = new
                       {
                           ShowName          = episode.ShowName,
                           SeasonEpisode     = episode.SeasonEpisode,
                           CleanedEpisode    = episode.CleanedShowName,
                           UpdateDate        = episode.UpdateDate,
                           WatchedDate       = episode.WatchedDate,
                           TvmEpisodeId      = episode.TvmEpisodeId,
                           TvmShowId         = episode.TvmShowId,
                           ProcessedToTvmaze = episode.ProcessedToTvmaze,
                       };
            json = JsonSerializer.Serialize(info);
            list.Add(json);
        }

        using StreamWriter file = new ("/Users/dick/TVMazeLinux/Inputs/WatchedEpisodes.log", append: true);

        foreach (var entry in list)
        {
            file.WriteLine(entry);
        }

        file.Close();
    }
}
