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
        Console.WriteLine($"{DateTime.Now}: {thisProgram} ");
        AppInfo appInfo = new("TVMaze", thisProgram, "DbAlternate");
        var     log     = appInfo.TxtFile;
        log.Start();
        LogModel.Start(thisProgram);

        Feed result = new();

        try
        {
            var showRssFeed = FeedReader.ReadAsync("http://showrss.info/user/2202.rss?magnets=true&namespaces=true&name=null&quality=null&re=null");
            showRssFeed.Wait();
            result = showRssFeed.Result;
            LogModel.Record(thisProgram, "Main", $"Received {result.Items.Count} from the RssShow feed", 3);
        }
        catch (Exception ex)
        {
            log.Write($"########################################## Exception during FeedReading: {ex}", "", 0);
            LogModel.Record(thisProgram, "Main", $"Error: with the RssShow feed {ex.Message}", 6);
            log.Stop();
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
                log.Write($"Found Repack or Proper Version: {show.Title}");
                LogModel.Record(thisProgram, "Main", $"Found Repack or Proper Version: {show.Title}", 3);
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

            log.Write($"Added {show.Title} to Transmission");
            LogModel.Record(thisProgram, "Main", $"Processing magnet for show: {show.Title}", 3);

            using var db          = new TvMaze();

            var recordToAdd = new ShowRssFeed
                              {
                                  ShowName   = show.Title,
                                  Processed  = true,
                                  Url        = show.Link,
                                  UpdateDate = DateTime.Now.ToString("yyy-MM-dd"),
                                  };
            db.ShowRssFeeds.Add(recordToAdd);
            db.SaveChanges();

            // var       sql         = "insert ShowRssFeed values (";
            // sql += "0, ";
            // sql += $"'{show.Title}', ";
            // sql += "0, ";
            // sql += $"'{show.Link}', ";
            // sql += $"'{DateTime.Now.Date:yyyy-MM-dd}') ";
            // var row = mdb.ExecNonQuery(sql);
            // mdb.Close();
            //log.Write(row != 1 ? $"Insert of Episode {show.Title} Failed" : $"Inserted Episode {show.Title} successfully", "", 4);
        }

        log.Write($"Processed {idx} records from ShowRss");
        LogModel.Record(thisProgram, "Main", $"Processed: {idx} ShowRss Feed records", 3);
        log.Stop();
        LogModel.Stop(thisProgram);
    }

    private static bool CheckIfProcessed(AppInfo appInfo, string showName)
    {
        using var db                 = new TvMaze();
        var       result             = db.ShowRssFeeds.SingleOrDefault(s => s.ShowName == showName);

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
