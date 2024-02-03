using System;
using System.Diagnostics;
using System.Linq;

using CodeHollow.FeedReader;

using Common_Lib;

using DB_Lib;

using DB_Lib_EF.Entities;
using DB_Lib_EF.Models.MariaDB;

namespace RefreshShowRssFeed;

internal static class RefreshShowRssFeed
{
    private static void Main()
    {
        const string thisProgram = "Refresh ShowRss Feed";
        AppInfo appInfo = new("TVMaze", thisProgram, "DbAlternate");
        LogModel.Start(thisProgram);

        Feed result = new();

        try
        {
            var showRssFeed = FeedReader.ReadAsync("http://showrss.info/user/2202.rss?magnets=true&namespaces=true&name=null&quality=null&re=null");
            showRssFeed.Wait();
            result = showRssFeed.Result;
            LogModel.Record(thisProgram, "Main", $"Received {result.Items.Count} from the RssShow feed", 1);
        }
        catch (Exception ex)
        {
            LogModel.Record(thisProgram, "Main", $"Error: with the RssShow feed {ex.Message}  ::: {ex.InnerException}", 20);
            LogModel.Stop(thisProgram);
            Environment.Exit(99);
        }

        MariaDb mdb = new(appInfo);
        var     idx = 0;

        foreach (var show in result.Items)
        {
            idx++;

            if (CheckIfProcessed(appInfo, show.Title))
            {
                LogModel.Record(thisProgram, "Main", $"Already processed {show.Title} before", 5);

                continue;
            }

            if (show.Title.ToLower().Contains("proper") || show.Title.ToLower().Contains("repack"))
            {
                LogModel.Record(thisProgram, "Main", $"Found Repack or Proper Version: {show.Title}", 1);
            }

            using (Process acquireMediaScript = new())
            {
                acquireMediaScript.StartInfo.FileName               = "/media/psf/TVMazeLinux/Scripts/TorrentToTransmission.sh";
                acquireMediaScript.StartInfo.Arguments              = show.Link;
                acquireMediaScript.StartInfo.UseShellExecute        = true;
                acquireMediaScript.StartInfo.RedirectStandardOutput = false;
                acquireMediaScript.Start();
                acquireMediaScript.WaitForExit();
            }

            LogModel.Record(thisProgram, "Main", $"Processing magnet for show: {show.Title}", 1);

            using var db = new TvMaze();

            var recordToAdd = new ShowRssFeed
                              {
                                  ShowName = show.Title, Processed = true, Url = show.Link, UpdateDate = DateTime.Now.ToString("yyy-MM-dd"),
                              };
            db.ShowRssFeeds.Add(recordToAdd);
            db.SaveChanges();
        }

        LogModel.Record(thisProgram, "Main", $"Processed: {idx} ShowRss Feed records", 1);
        LogModel.Stop(thisProgram);
    }

    private static bool CheckIfProcessed(AppInfo appInfo, string showName)
    {
        using var db     = new TvMaze();
        var       result = db.ShowRssFeeds.SingleOrDefault(s => s.ShowName == showName);

        if (result != null)
        {
            if (result.Processed.HasValue)
            {
                return result.Processed.Value;
            }
        }

        return false;
    }
}
