using Common_Lib;
using Entities_Lib;
using Newtonsoft.Json.Linq;
using System;
using Web_Lib;

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

            log.Stop();
            Console.WriteLine($"{DateTime.Now}: {This_Program} Finished");
        }
    }
}