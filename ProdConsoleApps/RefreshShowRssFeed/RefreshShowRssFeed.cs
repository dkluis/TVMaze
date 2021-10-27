using System;
using System.Diagnostics;
using CodeHollow.FeedReader;
using Common_Lib;
using DB_Lib;

namespace RefreshShowRssFeed
{
    internal class RefreshShowRssFeed
    {
        private static void Main(string[] args)
        {
            var thisProgram = "Refresh ShowRss Feed";
            Console.WriteLine($"{DateTime.Now}: {thisProgram} ");
            AppInfo appinfo = new("TVMaze", thisProgram, "DbAlternate");
            var log = appinfo.TxtFile;
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
                log.Write($"########################################## Exception duing FeedReading: {ex}", "", 0);
                log.Stop();
                Environment.Exit(99);
            }

            MariaDb mdb = new(appinfo);
            var sql = "";
            var row = 0;
            var idx = 0;

            foreach (var show in result.Items)
            {
                idx++;
                if (CheckIfProcessed(appinfo, show.Title)) continue;

                if (show.Title.ToLower().Contains("proper") || show.Title.ToLower().Contains("repack"))
                    log.Write($"Found Repack or Proper Version: {show.Title}");

                using (Process acquireMediaScript = new())
                {
                    acquireMediaScript.StartInfo.FileName = "/Users/dick/TVMaze/Scripts/AcquireMediaViaTransmission.sh";
                    acquireMediaScript.StartInfo.Arguments = show.Link;
                    acquireMediaScript.StartInfo.UseShellExecute = true;
                    acquireMediaScript.StartInfo.RedirectStandardOutput = false;
                    var started = acquireMediaScript.Start();
                    acquireMediaScript.WaitForExit();
                }

                log.Write($"Added {show.Title} to Transmission");

                sql = "insert ShowRssFeed values (";
                sql += "0, ";
                sql += $"'{show.Title}', ";
                sql += "0, ";
                sql += $"'{show.Link}', ";
                sql += $"'{DateTime.Now.ToString("yyyy-MM-dd")}') ";
                row = mdb.ExecNonQuery(sql);
                mdb.Close();
                if (row != 1)
                    log.Write($"Insert of Episode {show.Title} Failed", "", 4);
                else
                    log.Write($"Inserted Episode {show.Title} successfully", "", 4);
            }

            log.Write($"Processed {idx} records from ShowRss");
            log.Stop();
        }

        private static bool CheckIfProcessed(AppInfo appinfo, string showname)
        {
            var isProcessed = false;
            using (MariaDb mdbr = new(appinfo))
            {
                var rsql = $"select `Showname`, Processed from ShowRssFeed where `ShowName` = '{showname}' ";
                var rdr = mdbr.ExecQuery(rsql);
                if (rdr.HasRows) isProcessed = true;
                mdbr.Close();
            }

            return isProcessed;
        }
    }
}