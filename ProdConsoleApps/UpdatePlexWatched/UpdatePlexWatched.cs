using System;

using Common_Lib;

using DB_Lib;

using DB_Lib_EF.Entities;

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
        LogModel.Start(thisProgram);

        var watchedEpisodes = PlexSqlLite.PlexWatched(appInfo);

        if (watchedEpisodes.Count > 0)
        {
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
                        LogModel.Record(thisProgram, "Main", $"Found Show: {pwi.ShowName}", 5);

                        break;
                    }

                    case > 1:
                    {
                        foreach (var showId in foundInDb)
                        {
                            log.Write($"Multiple ShowIds found for {pwi.ShowName} is: {showId}", "", 1);
                            ActionItemModel.RecordActionItem(thisProgram, $"Multiple ShowIds found for {pwi.ShowName} is: {showId}", log);
                            LogModel.Record(thisProgram, "Main", $"Found multiple Shows named: {pwi.ShowName}", 5);
                        }

                        break;
                    }

                    default:
                    {
                        log.Write($"Did not find any ShowIds for {pwi.ShowName}", "", 1);
                        ActionItemModel.RecordActionItem(thisProgram, $"Did not find any ShowIds for {pwi.ShowName}", log);
                        LogModel.Record(thisProgram, "Main", $"NotFound Show: {pwi.ShowName}", 5);

                        break;
                    }
                }

                log.Write($"ShowId found for {pwi.ShowName}: ShowId: {pwi.TvmShowId}, EpisodeId: {pwi.TvmEpisodeId}", "", 4);
                LogModel.Record(thisProgram, "Main", $"ShowId found for {pwi.ShowName}: ShowId: {pwi.TvmShowId}, EpisodeId: {pwi.TvmEpisodeId}", 4);

                if (!pwi.DbInsert(appInfo)) continue;

                if (pwi.TvmEpisodeId == 0)
                {
                    log.Write($"TvmEpisodeId is 0 for {pwi.ShowName} - {pwi.SeasonEpisode}", "", 1);
                    LogModel.Record(thisProgram, "Main", $"TvmEpisodeId is 0 for {pwi.ShowName} - {pwi.SeasonEpisode}", 4);
                    ActionItemModel.RecordActionItem(thisProgram, $"TvmEpisodeId is 0 for {pwi.ShowName} - {pwi.SeasonEpisode}", log);

                    continue;
                }

                using Episode epi = new(appInfo);
                epi.FillViaTvmaze(pwi.TvmEpisodeId);
                using WebApi wa = new(appInfo);
                wa.PutEpisodeToWatched(epi.TvmEpisodeId, pwi.WatchedDate);
                epi.PlexDate   = pwi.WatchedDate;
                epi.PlexStatus = "Watched";
                epi.DbUpdate();
                log.Write($"Update Episode Record for Show {pwi.ShowName} {epi.TvmEpisodeId}, {epi.PlexDate}, {epi.PlexStatus}", "", 2);
                LogModel.Record(thisProgram, "Main", $"Update Episode Record for Show {pwi.ShowName} {epi.TvmEpisodeId}, {epi.PlexDate}, {epi.PlexStatus}", 3);
                pwi.ProcessedToTvmaze = true;
                pwi.DbUpdate(appInfo);

                if (epi.IsAutoDelete)
                {
                    log.Write($"Deleting this episode {pwi.ShowName} - {pwi.SeasonEpisode} file");
                    LogModel.Record(thisProgram, "Main", $"Deleting this episode {pwi.ShowName} - {pwi.SeasonEpisode} file", 3);
                    using MediaFileHandler mfh = new(appInfo);
                    _ = mfh.DeleteEpisodeFiles(epi);
                }

                pwi.Reset();
            }
        } else
        {
            log.Write("No Watched Episodes Found");
            LogModel.Record(thisProgram, "Main", "No Watched Episodes Found", 3);
        }

        log.Stop();
        LogModel.Stop(thisProgram);
    }
}
