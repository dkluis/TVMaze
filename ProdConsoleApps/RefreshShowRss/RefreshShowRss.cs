using Common_Lib;
using Entities_Lib;
using Web_Lib;

namespace RefreshShowRss;

internal static class RefreshShowRss
{
    private static void Main()
    {
        const string thisProgram = "Refresh ShowRss";
        Console.WriteLine($"{DateTime.Now}: {thisProgram}");
        AppInfo appInfo = new("TVMaze", thisProgram, "DbAlternate");
        var     log     = appInfo.TxtFile;
        log.Start();
        WebScrape           showScrape         = new(appInfo);
        var                 showRssShows       = showScrape.GetShowRssInfo();
        SearchShowsViaNames searchShowViaNames = new();
        UpdateFinder        updateFinder       = new();
        log.Write($"Found {showRssShows.Count} in the ShowRss HTML download");
        var idx = 1;
        foreach (var show in showRssShows)
        {
            log.Write($"On ShowRss: {show}", "", 4);
            var cleanShow = Common.RemoveSuffixFromShowName(Common.RemoveSpecialCharsInShowName(show));
            var foundInDb = searchShowViaNames.Find(appInfo, cleanShow);
            switch (foundInDb.Count)
            {
                case < 1:
                    log.Write($"Found {cleanShow} on ShowRSS but not in Followed/Shows", "", 2);
                    continue;
                case > 1:
                {
                    log.Write($"Found multiple shows {cleanShow} in DB Show Table");
                    foreach (var showId in foundInDb)
                    {
                        using Show showUpd = new(appInfo);
                        showUpd.FillViaTvmaze(showId);
                        if (showUpd.ShowStatus == "Running" && showUpd.UpdateDate != "2200-01-01")
                        {
                            log.Write($"Selected to Update {cleanShow}: {showUpd.TvmShowId} to Finder: ShowRss");
                            UpdateFinder.ToShowRss(appInfo, showUpd.TvmShowId);
                            idx++;
                        }

                        log.Write($"TvmShowId {showId}: {cleanShow}", "", 0);
                    }

                    idx--;
                    continue;
                }
            }

            log.Write($"Updating {cleanShow} to Finder: ShowRss", "", 4);
            UpdateFinder.ToShowRss(appInfo, int.Parse(foundInDb[0].ToString()));
            idx++;
        }

        log.Write($"Updated {idx} Shows to Finder ShowRss");
        log.Stop();
    }
}
