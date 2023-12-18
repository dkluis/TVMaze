using System;

using Common_Lib;

using DB_Lib;

using Entities_Lib;

using Web_Lib;

namespace UpdatePlexWatched;

/// <summary>
///     1. Reads the Plex Sqlite DB and get all Episodes that are marked Watched in the last day
///     2. Figures out from the Plex ShowName, season and episode numbers what TvmShowId and TvmEpisodeId it is in Tvmaze
///     Local
///     3. Updates Tvmaze Local and Tvmaze Web with the Watched status and the date
///     a. Adds a record in Tvmaze Local to track if an episode is already updated
///     4. Delete media from Plex if Auto Delete is set for that Show
/// </summary>
internal static class UpdatePlexWatched
{
    private static void Main()
    {
        const string thisProgram = "Update Plex Watched";
        Console.WriteLine($"{DateTime.Now}: {thisProgram}");
        AppInfo appInfo = new("TVMaze", thisProgram, "DbAlternate");
        var     log     = appInfo.TxtFile;

        log.Start();

        var watchedEpisodes = PlexSqlLite.PlexWatched(appInfo);

        if (watchedEpisodes.Count > 0)
        {
            log.Write($"{watchedEpisodes.Count} Watched Episodes Found");

            foreach (var pwi in watchedEpisodes)
            {
                SearchShowsViaNames searchShowViaNames = new();
                var                 foundInDb          = searchShowViaNames.Find(appInfo, pwi.ShowName, pwi.CleanedShowName);

                switch (foundInDb.Count)
                {
                    case 1:
                    {
                        pwi.TvmShowId = foundInDb[0];
                        using EpisodeSearch es = new();
                        pwi.TvmEpisodeId = es.Find(appInfo, pwi.TvmShowId, pwi.SeasonEpisode);

                        break;
                    }

                    case > 1:
                    {
                        foreach (var showId in foundInDb)
                        {
                            log.Write($"Multiple ShowIds found for {pwi.ShowName} is: {showId}", "", 1);
                            using ActionItems ais = new(appInfo);
                            ais.DbInsert($"Multiple ShowIds found for {pwi.ShowName} is: {showId}", true);
                        }

                        continue;
                    }

                    default:
                    {
                        log.Write($"Did not find any ShowIds for {pwi.ShowName}", "", 1);
                        using ActionItems ai = new(appInfo);
                        ai.DbInsert($"Did not find any ShowIds for {pwi.ShowName}", true);

                        continue;
                    }
                }

                log.Write($"ShowId found for {pwi.ShowName}: ShowId: {pwi.TvmShowId}, EpisodeId: {pwi.TvmEpisodeId}", "", 4);

                if (!pwi.DbInsert(appInfo)) continue;

                using Episode epi = new(appInfo);
                epi.FillViaTvmaze(pwi.TvmEpisodeId);
                using WebApi wa = new(appInfo);
                wa.PutEpisodeToWatched(epi.TvmEpisodeId, pwi.WatchedDate);
                epi.PlexDate   = pwi.WatchedDate;
                epi.PlexStatus = "Watched";
                epi.DbUpdate();
                log.Write($"Update Episode Record for Show {pwi.ShowName} {epi.TvmEpisodeId}, {epi.PlexDate}, {epi.PlexStatus}", "", 2);
                pwi.ProcessedToTvmaze = true;
                pwi.DbUpdate(appInfo);

                if (epi.IsAutoDelete)
                {
                    log.Write($"Deleting this episode {pwi.ShowName} - {pwi.SeasonEpisode} file");
                    using MediaFileHandler mfh = new(appInfo);
                    _ = mfh.DeleteEpisodeFiles(epi);
                }

                pwi.Reset();
            }
        } else
        {
            log.Write("No Watched Episodes Found");
        }

        log.Stop();
    }
}
