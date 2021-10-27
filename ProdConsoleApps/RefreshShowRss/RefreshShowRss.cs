using System;
using Common_Lib;
using Entities_Lib;
using Web_Lib;

namespace RefreshShowRss
{
    internal class RefreshShowRss
    {
        private static void Main()
        {
            var thisProgram = "Refresh ShowRss";
            Console.WriteLine($"{DateTime.Now}: {thisProgram}");
            AppInfo appinfo = new("TVMaze", thisProgram, "DbAlternate");
            var log = appinfo.TxtFile;
            log.Start();

            // Update All Shows with ShowRss Finder Info

            WebScrape showscrape = new(appinfo);
            var showRssShows = showscrape.GetShowRssInfo();
            SearchShowsViaNames ssvn = new();
            UpdateFinder uf = new();
            log.Write($"Found {showRssShows.Count} in the ShowRss HTML download");
            var idx = 1;
            foreach (var show in showRssShows)
            {
                log.Write($"On ShowRss: {show}", "", 4);
                var cleanshow = Common.RemoveSuffixFromShowName(Common.RemoveSpecialCharsInShowName(show));
                var foundindb = ssvn.Find(appinfo, cleanshow);
                if (foundindb.Count < 1)
                {
                    log.Write($"Found {cleanshow} on ShowRSS but not in Followed/Shows", "", 2);
                    continue;
                }

                if (foundindb.Count > 1)
                {
                    log.Write($"Found multiple shows {cleanshow} in DB Show Table");
                    foreach (var showid in foundindb)
                    {
                        using (Show showdup = new(appinfo))
                        {
                            showdup.FillViaTvmaze(showid);
                            if (showdup.ShowStatus == "Running")
                            {
                                log.Write($"Selected to Update {cleanshow}: {showdup.TvmShowId} to Finder: ShowRss");
                                uf.ToShowRss(appinfo, showdup.TvmShowId);
                                idx++;
                            }
                        }

                        log.Write($"TvmShowId {showid}: {cleanshow}", "", 0);
                    }

                    idx--;
                    continue;
                }

                log.Write($"Updating {cleanshow} to Finder: ShowRss", "", 4);
                uf.ToShowRss(appinfo, int.Parse(foundindb[0].ToString()));
                idx++;
            }

            log.Write($"Updated {idx} Shows to Finder ShowRss");

            log.Stop();
        }
    }
}