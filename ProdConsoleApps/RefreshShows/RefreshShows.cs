using System;
using System.Collections.Generic;

using Common_Lib;
using Web_Lib;
using TvmEntities;

using Newtonsoft.Json.Linq;

namespace RefreshShows
{
    class RefreshShows
    {
        static void Main()
        {
            string This_Program = "Refresh Shows";
            Console.WriteLine($"{DateTime.Now}: Starting {This_Program}");
            AppInfo appinfo = new("TVMaze", This_Program, "DbAlternate");
            Console.WriteLine($"{DateTime.Now}: Progress can be followed in {appinfo.FullPath}");
            TextFileHandler log = appinfo.TxtFile;
            log.Start();

            // Update Shows table with all Followed Shows

            WebAPI tvmapi = new(appinfo);
            JArray jsoncontent = tvmapi.ConvertHttpToJArray(tvmapi.GetFollowedShows());
            Show iu_show = new(appinfo);

            int iu_idx = 0;
            foreach (JToken show in jsoncontent)
            {
                iu_show.FillViaTvmaze(Int32.Parse(show["show_id"].ToString()));
                if (iu_show.isDBFilled)
                {
                    log.Write($"Updating Shows Table with {show["show_id"]}");
                    iu_show.DbUpdate();
                    iu_idx++;
                    iu_show.Reset();
                }
                else
                {
                    log.Write($"Inserting into Shows Table with {show["show_id"]}");
                    iu_show.DbInsert();
                    iu_idx++;
                    iu_show.Reset();
                }
            }
            log.Write($"Updated {iu_idx} Show records");


            // Update All Shows with ShowRss Finder Info

            WebScrape showscrape = new(appinfo);
            List<string> ShowRssShows = showscrape.GetShowRssInfo();
            SearchShowsViaNames ssvn = new();
            UpdateFinder UF = new();
            log.Write($"Found {ShowRssShows.Count} in the ShowRss HTML download");
            int idx = 1;
            foreach (string show in ShowRssShows)
            {
                // log.Write($"On ShowRss: {show}", "", 4);
                List<int> foundindb = ssvn.Find(appinfo, show);
                if (foundindb.Count < 1) { continue; }
                if (foundindb.Count > 1)
                {
                    log.Write($"Found multiple shows {show} in DB Show Table");
                    foreach (int showid in foundindb)
                    {
                        log.Write($"TvmShowId {showid}: {show}", "", 0);
                    }
                    idx--;
                    continue;
                }
                log.Write($"Updating {show} to Finder: ShowRss", "", 4);
                UF.ToShowRss(appinfo, Int32.Parse(foundindb[0].ToString()));
                idx++;
            }
            log.Write($"Updated {idx} Shows to Finder ShowRss");

            log.Stop();
            Console.WriteLine($"{DateTime.Now}: Finished {This_Program} Program");
        }
    }
}