using System;

using CodeHollow.FeedReader;
using System.Threading.Tasks;

using Common_Lib;
using DB_Lib;
using System.Diagnostics;

namespace RefreshShowRssFeed
{

    class RefreshShowRssFeed
    {
        static void Main(string[] args)
        {
            string This_Program = "Refresh ShowRss Feed";
            Console.WriteLine($"{DateTime.Now}: {This_Program} Started");
            AppInfo appinfo = new("TVMaze", This_Program, "DbAlternate");
            Console.WriteLine($"{DateTime.Now}: {This_Program} Progress can be followed in {appinfo.FullPath}");
            TextFileHandler log = appinfo.TxtFile;
            log.Start();

            Task<Feed> ShowRssFeed = FeedReader.ReadAsync("http://showrss.info/user/2202.rss?magnets=true&namespaces=true&name=null&quality=null&re=null");
            ShowRssFeed.Wait();
            Feed Result = ShowRssFeed.Result;

            MariaDB Mdb = new(appinfo);
            string sql = "";
            int row = 0;

            foreach (var Show in Result.Items)
            {
                if (CheckIfProcessed(appinfo, Show.Title))
                {
                    continue;
                }
                
                using (Process AcquireMediaScript = new())
                {
                    AcquireMediaScript.StartInfo.FileName = "/Users/dick/TVMaze/Scripts/AcquireMediaViaTransmission.sh";
                    AcquireMediaScript.StartInfo.Arguments = Show.Link;
                    AcquireMediaScript.StartInfo.UseShellExecute = true;
                    AcquireMediaScript.StartInfo.RedirectStandardOutput = false;
                    bool started = AcquireMediaScript.Start();
                    AcquireMediaScript.WaitForExit();
                }
                log.Write($"Added {Show.Title} to Transmission");

                sql = "insert ShowRssFeed values (";
                sql += $"0, ";
                sql += $"'{Show.Title}', ";
                sql += $"0, ";
                sql += $"'{Show.Link}', ";
                sql += $"'{DateTime.Now.ToString("yyyy-MM-dd")}') ";
                row = Mdb.ExecNonQuery(sql);
                Mdb.Close();
                if (row != 1) { log.Write($"Insert of Episode {Show.Title} Failed", "", 4); } else { log.Write($"Inserted of Episode {Show.Title} successfully", "", 4); }
            }


            log.Stop();
            Console.WriteLine($"{DateTime.Now}: {This_Program} Finished");
        }

        static bool CheckIfProcessed(AppInfo appinfo, string showname)
        {
            bool isProcessed = false;
            using (MariaDB Mdbr = new(appinfo))
            {
                string rsql = $"select `Showname`, Processed from ShowRssFeed where `ShowName` = '{showname}' ";
                MySqlConnector.MySqlDataReader rdr = Mdbr.ExecQuery(rsql);
                if (rdr.HasRows)
                {
                    isProcessed = true;
                }
                Mdbr.Close();
            }
            return isProcessed;
        }
    }
}
