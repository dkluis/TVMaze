﻿using Common_Lib;
using Entities_Lib;
using System;
using System.Collections.Generic;
using Web_Lib;


namespace RefreshShowRss
{
    class RefreshShowRss
    {
        static void Main()
        {
            string This_Program = "Refresh ShowRss";
            Console.WriteLine($"{DateTime.Now}: {This_Program} Started");
            AppInfo appinfo = new("TVMaze", This_Program, "DbAlternate");
            Console.WriteLine($"{DateTime.Now}: {This_Program} Progress can be followed in {appinfo.FullPath}");
            TextFileHandler log = appinfo.TxtFile;
            log.Start();

            // Update All Shows with ShowRss Finder Info

            WebScrape showscrape = new(appinfo);
            List<string> ShowRssShows = showscrape.GetShowRssInfo();
            SearchShowsViaNames ssvn = new();
            UpdateFinder UF = new();
            log.Write($"Found {ShowRssShows.Count} in the ShowRss HTML download");
            int idx = 1;
            foreach (string show in ShowRssShows)
            {
                // log.Write($"On ShowRss: {show}", "", 4);
                List<int> foundindb = ssvn.Find(appinfo, show);
                if (foundindb.Count < 1) { log.Write($"Found {show} on ShowRSS but not in Followed/Shows", "", 2); continue; }
                if (foundindb.Count > 1)
                {
                    log.Write($"Found multiple shows {show} in DB Show Table");
                    foreach (int showid in foundindb)
                    {
                        log.Write($"TvmShowId {showid}: {show}", "", 0);
                    }
                    idx--;
                    continue;
                }
                log.Write($"Updating {show} to Finder: ShowRss", "", 4);
                UF.ToShowRss(appinfo, Int32.Parse(foundindb[0].ToString()));
                idx++;
            }
            log.Write($"Updated {idx} Shows to Finder ShowRss");

            log.Stop();
            Console.WriteLine($"{DateTime.Now}: {This_Program} Finished");
        }
    }
}