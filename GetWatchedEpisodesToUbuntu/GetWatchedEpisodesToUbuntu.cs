using System.Text.Json;

namespace GetWatchedEpisodesToUbuntu;

internal static class UpdatePlexWatchedMacOs
{
    private static void Main()
    {
        var watchedEpisodes = SqlLiteMacOs.PlexWatched();
        var list            = new List<string>();

        foreach (var episode in watchedEpisodes)
        {
            var info = new
                       {
                           episode.ShowName, episode.SeasonEpisode,
                           CleanedEpisode = episode.CleanedShowName, episode.UpdateDate, episode.WatchedDate, episode.TvmEpisodeId, episode.TvmShowId, episode.ProcessedToTvmaze,
                       };
            var json = JsonSerializer.Serialize(info);
            list.Add(json);
        }

        using StreamWriter file = new("/Users/dick/TVMazeLinux/Inputs/WatchedEpisodes.log", true);

        foreach (var entry in list) file.WriteLine(entry);

        file.Close();
    }
}
