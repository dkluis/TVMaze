using System;
using System.Collections.Generic;

using MySqlConnector;

using Common_Lib;

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
                    log.Write($"{pwi.ShowName}, {pwi.Season}, {pwi.Episode}, {pwi.WatchedDate}");
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