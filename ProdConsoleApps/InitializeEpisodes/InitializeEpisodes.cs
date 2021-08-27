using Common_Lib;
using Entities_Lib;

using System;
using System.Collections.Generic;

namespace InitializeEpisodes
{
    class InitializeEpisodes
    {
        static void Main()
        {
            string This_Program = "Init Episode Table";
            Console.WriteLine($"{DateTime.Now}:  {This_Program} Started");
            AppInfo appinfo = new("TVMaze", This_Program, "DbAlternate");
            Console.WriteLine($"{DateTime.Now}:  {This_Program} Progress can be followed in {appinfo.FullPath}");
            TextFileHandler log = appinfo.TxtFile;
            log.Start();

            //Get All Followed Shows
            List<int> allfollowed = new();
            SearchAllFollowed sal = new();
            allfollowed = sal.Find(appinfo);
            if (allfollowed.Count == 0)
            {
                log.Write($"No Followed Shows Found, exiting program", "", 0);
                Environment.Exit(99);
            }
            log.Write($"Found {allfollowed.Count} Show to get Episodes for");

            //Process All Episodes for the Followed Shows
            int idxalleps = 0;
            foreach (int showid in allfollowed)
            {
                // TODO take when fully tested --- if (showid < 43) { continue; } else if (showid > 43) { Environment.Exit(99); }
                int idxepsbyshow = 0;
                EpisodesByShow epsbyshow = new();
                List<Episode> ebs = epsbyshow.Find(appinfo, showid);
                foreach (Episode eps in ebs)
                {
                    eps.DbInsert();
                    log.Write($"Inserted Episode {eps.TvmShowId}, {eps.ShowName}, {eps.TvmEpisodeId}, {eps.SeasonEpisode}", "", 4);
                    idxalleps++;
                    idxepsbyshow++;
                }
                log.Write($"Number of Episodes for Show {showid}: {idxepsbyshow}", "", 2);
            }

            log.Write($"Number of All Episodes for All Shows: {idxalleps}");
            log.Stop();
            Console.WriteLine($"{DateTime.Now}: {This_Program} Finished");
        }
    }
}