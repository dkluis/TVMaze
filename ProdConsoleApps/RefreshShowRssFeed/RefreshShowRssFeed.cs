using System;
using System.Diagnostics;
using CodeHollow.FeedReader;
using Common_Lib;
using DB_Lib;

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

        Feed result = new();
        try
        {
            var showRssFeed =
                FeedReader.ReadAsync(
                                     "http://showrss.info/user/2202.rss?magnets=true&namespaces=true&name=null&quality=null&re=null");
            showRssFeed.Wait();
            result = showRssFeed.Result;
        }
        catch (Exception ex)
        {
            log.Write($"########################################## Exception during FeedReading: {ex}", "", 0);
            log.Stop();
            Environment.Exit(99);
        }

        MariaDb mdb = new(appInfo);
        var     idx = 0;

        foreach (var show in result.Items)
        {
            idx++;
            if (CheckIfProcessed(appInfo, show.Title)) continue;

            if (show.Title.ToLower().Contains("proper") || show.Title.ToLower().Contains("repack"))
                log.Write($"Found Repack or Proper Version: {show.Title}");

            using (Process acquireMediaScript = new())
            {
                acquireMediaScript.StartInfo.FileName               = "/Users/dick/TVMaze/Scripts/AcquireMediaViaTransmission.sh";
                acquireMediaScript.StartInfo.Arguments              = show.Link;
                acquireMediaScript.StartInfo.UseShellExecute        = true;
                acquireMediaScript.StartInfo.RedirectStandardOutput = false;
                acquireMediaScript.Start();
                acquireMediaScript.WaitForExit();
            }

            log.Write($"Added {show.Title} to Transmission");

            var sql = "insert ShowRssFeed values (";
            sql += "0, ";
            sql += $"'{show.Title}', ";
            sql += "0, ";
            sql += $"'{show.Link}', ";
            sql += $"'{DateTime.Now.Date:yyyy-MM-dd}') ";
            var row = mdb.ExecNonQuery(sql);
            mdb.Close();
            log.Write(
                      row != 1 ? $"Insert of Episode {show.Title} Failed" : $"Inserted Episode {show.Title} successfully",
                      "", 4);
        }

        log.Write($"Processed {idx} records from ShowRss");
        log.Stop();
    }
    private static bool CheckIfProcessed(AppInfo appInfo, string showName)
    {
        var           isProcessed    = false;
        using MariaDb mDbR           = new(appInfo);
        var           rSql           = $"select `ShowName`, Processed from ShowRssFeed where `ShowName` = '{showName}' ";
        var           rdr            = mDbR.ExecQuery(rSql);
        if (rdr.HasRows) isProcessed = true;
        mDbR.Close();

        return isProcessed;
    }
}
