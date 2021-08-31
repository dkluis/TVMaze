using System;

using Common_Lib;
using Entities_Lib;
using Web_Lib;

using Newtonsoft.Json.Linq;

namespace RefreshShows
{
    class RefreshShows
    {
        static void Main()
        {
            string This_Program = "Refresh Shows";
            Console.WriteLine($"{DateTime.Now}: {This_Program} Started");
            AppInfo appinfo = new("TVMaze", This_Program, "DbAlternate");
            Console.WriteLine($"{DateTime.Now}: {This_Program} Progress can be followed in {appinfo.FullPath}");
            TextFileHandler log = appinfo.TxtFile;
            log.Start();

            WebAPI tvmapi = new(appinfo);
            JArray jsoncontent = tvmapi.ConvertHttpToJArray(tvmapi.GetFollowedShows());

            int iu_idx = 0;

            foreach (JToken show in jsoncontent)
            {
                using (ShowAndEpisodes sae = new(appinfo))
                {
                    log.Write($"Working on {show["show_id"]}");
                    sae.Refresh(int.Parse(show["show_id"].ToString()));
                    System.Threading.Thread.Sleep(1000);
                }
            }

            log.Write($"Updated {iu_idx} Show records");

            log.Stop();
            Console.WriteLine($"{DateTime.Now}: {This_Program} Finished");
        }
    }
}