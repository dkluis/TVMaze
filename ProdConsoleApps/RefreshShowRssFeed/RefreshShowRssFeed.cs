using System;
using System.Diagnostics;
using System.Linq;
using CodeHollow.FeedReader;
using DB_Lib_EF.Entities;
using DB_Lib_EF.Models.MariaDB;
using Entities_Lib;

namespace RefreshShowRssFeed;

internal static class RefreshShowRssFeed
{
    private static void Main()
    {
        const string thisProgram = "Refresh ShowRss Feed";
        if (!LogModel.IsSystemActive())
        {
            LogModel.InActive(thisProgram);
            Environment.Exit(99);
        }
        LogModel.Start(thisProgram);
        Feed result = new();

        try
        {
            var showRssFeed = FeedReader.ReadAsync("http://showrss.info/user/2202.rss?magnets=true&namespaces=true&name=null&quality=null&re=null");
            showRssFeed.Wait();
            result = showRssFeed.Result;
            var count = result.Items.Count;
            LogModel.Record(thisProgram, "Main", "Received from the RssShow feed " + count + " records");
        }
        catch (Exception ex)
        {
            LogModel.Record(thisProgram, "Main", $"Error: with the RssShow feed {ex.Message}  ::: {ex.InnerException}", 20);
            LogModel.Stop(thisProgram);
            Environment.Exit(99);
        }

        var idx = 0;
        foreach (var feedRec in result.Items)
        {
            idx++;
            var processedRec = CheckIfProcessed(feedRec.Title);
            if (processedRec != null)
            {
                LogModel.Record(thisProgram, "Main", $"Already processed {feedRec.Title} before", 5);
                continue;
            }

            var showInfo = feedRec.Title.Replace(" ", ".");
            if (feedRec.Title.Contains("proper", StringComparison.CurrentCultureIgnoreCase) || feedRec.Title.Contains("repack", StringComparison.CurrentCultureIgnoreCase))
            {
                LogModel.Record(thisProgram, "Main", $"Found Repack or Proper Version: {feedRec.Title}", 2);
            }

            var foundInfo = GeneralMethods.FindShowEpisodeInfo(thisProgram, showInfo);
            if (foundInfo is {Found: false, TvmShowId: 0})
            {
                LogModel.Record(thisProgram, "Main", $"Show Episode not found: TvmShowId: {foundInfo.TvmShowId} - {foundInfo.Message}", 3);
                ActionItemModel.RecordActionItem(thisProgram, $"Show Episode not found: TvmShowId: {foundInfo.TvmShowId} - {foundInfo.Message} ::: Check Alt Name");
                if (processedRec == null)
                {
                    AddToShowRssFeed(feedRec.Title, feedRec.Link, false);
                } else
                {
                    UpdateShowRssFeed(processedRec);
                }
                continue;
            }
            if (foundInfo.Message.Contains("Found via") && foundInfo is {IsWatched: true, IsSeason: true})
            {
                LogModel.Record(thisProgram, "Main", $"For whole Season and found via: {foundInfo.Message} and IsWatched: {foundInfo.IsWatched} ", 3);
                continue;
            }
            if (foundInfo.IsWatched)
            {
                LogModel.Record(thisProgram, "Main", $"Show Episode already watched: {foundInfo.Message}", 3);
                continue;
            }

            LogModel.Record(thisProgram, "Main", $"Adding torrent to Transmission for {foundInfo.TvmShowId} with epi {foundInfo.TvmEpisodeId}", 3);
            using (Process acquireMediaScript = new())
            {
                acquireMediaScript.StartInfo.FileName = "/media/psf/TVMazeLinux/Scripts/TorrentToTransmission.sh";
                acquireMediaScript.StartInfo.Arguments = feedRec.Link;
                acquireMediaScript.StartInfo.UseShellExecute = true;
                acquireMediaScript.StartInfo.RedirectStandardOutput = false;
                acquireMediaScript.Start();
                acquireMediaScript.WaitForExit();
            }
            LogModel.Record(thisProgram, "Main", $"Processing magnet for show: {feedRec.Title}");
            AddToShowRssFeed(feedRec.Title, feedRec.Link, true);
        }

        LogModel.Record(thisProgram, "Main", $"Processed: {idx} ShowRss Feed records");
        LogModel.Stop(thisProgram);
    }

    private static ShowRssFeed? CheckIfProcessed(string showName)
    {
        using var db = new TvMaze();
        return db.ShowRssFeeds.SingleOrDefault(s => s.ShowName == showName);
    }

    private static void AddToShowRssFeed(string showName, string link, bool processed)
    {
        using var db = new TvMaze();
        var recordToAdd = new ShowRssFeed {ShowName = showName, Processed = processed, Url = link, UpdateDate = DateTime.Now.ToString("yyy-MM-dd")};
        db.ShowRssFeeds.Add(recordToAdd);
        db.SaveChanges();
        LogModel.Record("Refresh ShowRss Feed", "Main", $"Added the ShowRssFeed record for {showName}", 2);
    }

    private static void UpdateShowRssFeed(ShowRssFeed rec)
    {
        using var db = new TvMaze();
        var recToUpdate = db.ShowRssFeeds.SingleOrDefault(s => s.ShowName == rec.ShowName && s.Processed == false);
        if (recToUpdate != null)
        {
            recToUpdate.Processed = true;
            recToUpdate.UpdateDate = DateTime.Now.ToString("yyy-MM-dd");
            db.SaveChanges();
            return;
        }
        LogModel.Record("Refresh ShowRss Feed", "Main", $"Tried to update {rec.ShowName} for processed false and {rec.UpdateDate} but was not found", 20);
        ActionItemModel.RecordActionItem("Refresh ShowRss Feed", $"Tried to update {rec.ShowName} for processed false and {rec.UpdateDate} but was not found");
    }
}
