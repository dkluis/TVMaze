using Common_Lib;
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
            Console.WriteLine($"{DateTime.Now}: {This_Program}");
            AppInfo appinfo = new("TVMaze", This_Program, "DbAlternate");
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
                log.Write($"On ShowRss: {show}", "", 4);
                string cleanshow = Common.RemoveSuffixFromShowname(Common.RemoveSpecialCharsInShowname(show));
                List<int> foundindb = ssvn.Find(appinfo, cleanshow);
                if (foundindb.Count < 1) { log.Write($"Found {cleanshow} on ShowRSS but not in Followed/Shows", "", 2); continue; }
                if (foundindb.Count > 1)
                {
                    log.Write($"Found multiple shows {cleanshow} in DB Show Table");
                    foreach (int showid in foundindb)
                    {
                        using (Show showdup = new(appinfo))
                        {
                            showdup.FillViaTvmaze(showid);
                            if (showdup.ShowStatus == "Running")
                            {
                                log.Write($"Selected to Update {cleanshow}: {showdup.TvmShowId} to Finder: ShowRss", "", 3);
                                UF.ToShowRss(appinfo, showdup.TvmShowId);
                                idx++;
                            }
                        }
                        log.Write($"TvmShowId {showid}: {cleanshow}", "", 0);
                    }
                    idx--;
                    continue;
                }
                log.Write($"Updating {cleanshow} to Finder: ShowRss", "", 4);
                UF.ToShowRss(appinfo, Int32.Parse(foundindb[0].ToString()));
                idx++;
            }
            log.Write($"Updated {idx} Shows to Finder ShowRss");

            log.Stop();
        }
    }
}