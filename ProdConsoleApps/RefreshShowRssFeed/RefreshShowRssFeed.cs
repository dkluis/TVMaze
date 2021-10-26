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
            var This_Program = "Refresh ShowRss Feed";
            Console.WriteLine($"{DateTime.Now}: {This_Program} ");
            AppInfo appinfo = new("TVMaze", This_Program, "DbAlternate");
            var log = appinfo.TxtFile;
            log.Start();

            Feed Result = new();
            try
            {
                var ShowRssFeed =
                    FeedReader.ReadAsync(
                        "http://showrss.info/user/2202.rss?magnets=true&namespaces=true&name=null&quality=null&re=null");
                ShowRssFeed.Wait();
                Result = ShowRssFeed.Result;
            }
            catch (Exception ex)
            {
                log.Write($"########################################## Exception duing FeedReading: {ex}", "", 0);
                log.Stop();
                Environment.Exit(99);
            }

            MariaDB Mdb = new(appinfo);
            var sql = "";
            var row = 0;
            var idx = 0;

            foreach (var Show in Result.Items)
            {
                idx++;
                if (CheckIfProcessed(appinfo, Show.Title)) continue;

                if (Show.Title.ToLower().Contains("proper") || Show.Title.ToLower().Contains("repack"))
                    log.Write($"Found Repack or Proper Version: {Show.Title}");

                using (Process AcquireMediaScript = new())
                {
                    AcquireMediaScript.StartInfo.FileName = "/Users/dick/TVMaze/Scripts/AcquireMediaViaTransmission.sh";
                    AcquireMediaScript.StartInfo.Arguments = Show.Link;
                    AcquireMediaScript.StartInfo.UseShellExecute = true;
                    AcquireMediaScript.StartInfo.RedirectStandardOutput = false;
                    var started = AcquireMediaScript.Start();
                    AcquireMediaScript.WaitForExit();
                }

                log.Write($"Added {Show.Title} to Transmission");

                sql = "insert ShowRssFeed values (";
                sql += "0, ";
                sql += $"'{Show.Title}', ";
                sql += "0, ";
                sql += $"'{Show.Link}', ";
                sql += $"'{DateTime.Now.ToString("yyyy-MM-dd")}') ";
                row = Mdb.ExecNonQuery(sql);
                Mdb.Close();
                if (row != 1)
                    log.Write($"Insert of Episode {Show.Title} Failed", "", 4);
                else
                    log.Write($"Inserted Episode {Show.Title} successfully", "", 4);
            }

            log.Write($"Processed {idx} records from ShowRss");
            log.Stop();
        }

        private static bool CheckIfProcessed(AppInfo appinfo, string showname)
        {
            var isProcessed = false;
            using (MariaDB Mdbr = new(appinfo))
            {
                var rsql = $"select `Showname`, Processed from ShowRssFeed where `ShowName` = '{showname}' ";
                var rdr = Mdbr.ExecQuery(rsql);
                if (rdr.HasRows) isProcessed = true;
                Mdbr.Close();
            }

            return isProcessed;
        }
    }
}