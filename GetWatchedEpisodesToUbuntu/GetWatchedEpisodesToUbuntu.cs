using System.Text.Json;

namespace GetWatchedEpisodesToUbuntu;

internal static class UpdatePlexWatchedMacOs
{
    private static void Main()
    {
        var watchedEpisodes = SqlLiteMacOs.PlexWatched();
        var list = watchedEpisodes.Select(
                                           episode => new
                                           {
                                               episode.ShowName,
                                               episode.SeasonEpisode,
                                               episode.CleanedShowName,
                                               episode.UpdateDate,
                                               episode.WatchedDate,
                                               episode.TvmEpisodeId,
                                               episode.TvmShowId,
                                               episode.ProcessedToTvmaze,
                                           }
                                   )
                                  .Select(info => JsonSerializer.Serialize(info))
                                  .ToList();
        using StreamWriter file = new("/Users/dick/TVMazeLinux/Inputs/WatchedEpisodes.log", true);
        foreach (var entry in list)
        {
            file.WriteLine(entry);
        }
        file.Close();
    }
}
