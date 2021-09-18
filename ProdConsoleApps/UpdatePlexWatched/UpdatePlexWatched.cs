using System;
using System.Collections.Generic;

using MySqlConnector;

using Common_Lib;
using Entities_Lib;
using Web_Lib;

namespace DB_Lib
{

    /// <summary>
    ///
    ///     1. Reads the Plex Sqlite DB and get all Episodes that are marked Watched in the last day
    ///     2. Figures out from the Plex Showname, season and episode numbers what TvmShowId and TvmEpisodeId it is in Tvmaze Local
    ///     3. Updates Tvmaze Local and Tvmaze Web with the Watched status and the date
    ///         a. Adds a record in Tvmaze Local to track if an episode is already updated
    ///     4. Delete media from Plex if Auto Delete is set for that Show
    /// 
    /// </summary>
    class UpdatePlexWatched
    {
        static void Main()
        {
            string This_Program = "Update Plex Watched";
            Console.WriteLine($"{DateTime.Now}: {This_Program}");
            AppInfo appinfo = new("TVMaze", This_Program, "DbAlternate");
            TextFileHandler log = appinfo.TxtFile;

            log.Start();

            PlexSqlLite pdb = new();
            List<PlexWatchedInfo> watchedepisodes = pdb.PlexWatched(appinfo);

            if (watchedepisodes.Count > 0)
            {
                log.Write($"{watchedepisodes.Count} Watched Episodes Found");
                foreach (PlexWatchedInfo pwi in watchedepisodes)
                {
                    SearchShowsViaNames ssvn = new();
                    List<int> foundindb = ssvn.Find(appinfo, pwi.ShowName, pwi.CleanedShowName);
                    if (foundindb.Count == 1)
                    {
                        pwi.TvmShowId = foundindb[0];
                        using (EpisodeSearch es = new())
                        {
                            pwi.TvmEpisodeId = es.Find(appinfo, pwi.TvmShowId, pwi.SeasonEpisode);
                        }
                    }
                    else if (foundindb.Count > 1)
                    {
                        foreach (int showid in foundindb)
                        {
                            log.Write($"Multiple ShowIds found for {pwi.ShowName} is: {showid}", "", 1);
                            using (ActionItems ais = new(appinfo))
                            {
                                ais.DbInsert($"Multiple ShowIds found for {pwi.ShowName} is: {showid}", true);
                            }
                        }
                        break;
                    }
                    else
                    {
                        log.Write($"Did not find any ShowIds for {pwi.ShowName}", "", 1);
                        using (ActionItems ai = new(appinfo))
                        {
                            ai.DbInsert($"Did not find any ShowIds for {pwi.ShowName}", true);
                        }
                        break;
                    }
                    log.Write($"ShowId found for {pwi.ShowName}: ShowId: {pwi.TvmShowId}, EpisodeId: {pwi.TvmEpisodeId}", "", 4);
                    if (pwi.DbInsert(appinfo))
                    {
                        using (Episode epi = new(appinfo))
                        {
                            epi.FillViaTvmaze(pwi.TvmEpisodeId);
                            using (WebAPI wa = new(appinfo)) { wa.PutEpisodeToWatched(epi.TvmEpisodeId, pwi.WatchedDate); }
                            epi.PlexDate = pwi.WatchedDate;
                            epi.PlexStatus = "Watched";
                            epi.DbUpdate();
                            log.Write($"Update Episode Record for Show {pwi.ShowName} {epi.TvmEpisodeId}, {epi.PlexDate}, {epi.PlexStatus}", "", 2);

                            pwi.ProcessedToTvmaze = true;
                            pwi.DbUpdate(appinfo);
                            pwi.Reset();

                            if (epi.isAutoDelete)
                            {
                                log.Write($"Deleting this episode for {pwi.ShowName} - {pwi.SeasonEpisode} file", "", 3);
                                using (MediaFileHandler mfh = new(appinfo))
                                {
                                    _ = mfh.DeleteEpisodeFiles(epi);
                                }
                            }
                        }
                    } 
                }
            }
            else
            {
                log.Write($"No Watched Episodes Found");
            }

            log.Stop();
        }
    }
}