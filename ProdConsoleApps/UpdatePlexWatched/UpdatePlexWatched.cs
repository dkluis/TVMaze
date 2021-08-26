using System;
using System.Collections.Generic;

using MySqlConnector;

using Common_Lib;
using Entities_Lib;

namespace DB_Lib
{
    class UpdatePlexWatched
    {
        static void Main()
        {
            string This_Program = "Update Plex Watched";
            Console.WriteLine($"{DateTime.Now}: {This_Program} Started");
            AppInfo appinfo = new("TVMaze", This_Program, "DB Needed from Config");
            Console.WriteLine($"{DateTime.Now}: {This_Program} Progress can be followed in {appinfo.FullPath}");
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
                    // log.Write($"{pwi.ShowName}, {pwi.Season}, {pwi.Episode}, {pwi.WatchedDate}, {pwi.CleanedShowName}");
                    List<int> foundindb = ssvn.Find(appinfo, pwi.ShowName, pwi.CleanedShowName);
                    if (foundindb.Count == 1)
                    {
                        pwi.TvmShowId = foundindb[0];
                        using (EpisodeSearch es = new())
                        {
                            pwi.TvmEpisodeId = es.Find(appinfo, pwi.TvmShowId, pwi.SeasonEpisode );
                        }
                    }
                    else if (foundindb.Count > 1)
                    {
                        foreach (int showid in foundindb)
                        {
                            log.Write($"Multiple ShowIds found for {pwi.ShowName} is: {showid}", "", 1);
                        }
                    }
                    else
                    {
                        log.Write($"Did not find any ShowIds for {pwi.ShowName}");
                    }
                    log.Write($"ShowId found for {pwi.ShowName}: ShowId: {pwi.TvmShowId}, EpisodeId: {pwi.TvmEpisodeId}", "", 4);
                }
            }
            else
            {
                log.Write($"No Watched Episodes Found");
            }

            log.Stop();
            Console.WriteLine($"{DateTime.Now}: {This_Program} Finished");
        }
    }
}