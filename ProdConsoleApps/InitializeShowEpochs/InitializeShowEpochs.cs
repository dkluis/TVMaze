using System;

using Common_Lib;
using Web_Lib;
using DB_Lib;
using TvmEntities;

using Newtonsoft.Json.Linq;

namespace InitializeShowEpochs
{
    class InitializeShowEpochs
    {
        static void Main()
        {
            string This_Program = "Init Show Epochs";
            Console.WriteLine($"Starting {This_Program}");
            AppInfo appinfo = new("TVMaze", This_Program, "DbAlternate");
            Console.WriteLine($"Progress can be followed in {appinfo.FullPath}");
            TextFileHandler log = appinfo.TxtFile;
            log.Start();

            WebAPI tvmapi = new(appinfo);
            JArray jsoncontent = tvmapi.ConvertHttpToJArray(tvmapi.GetFollowedShows());
            Show iu_show = new(appinfo);

            int iu_idx = 0;
            MariaDB Mdbw = new(appinfo);

            foreach (JToken show in jsoncontent)
            {
                if (show["show_id"] is not null)
                {
                    int showid = Int32.Parse(show["show_id"].ToString());
                    //log.Write($"insert into TvmShowUpdates values (0, {showid}, 0, '{DateTime.Now:yyyy-MM-dd}');");
                    int rows = Mdbw.ExecNonQuery($"insert into TvmShowUpdates values (0, {showid}, 1, '{DateTime.Now:yyyy-MM-dd}');");
                    if (rows == 0) { log.Write($"Insert went wrong for {showid}"); }
                }
                else { log.Write($"JToken Show[id] was null", "", 0); }
                iu_idx++;
            }

            log.Write($"Processed {iu_idx} Followed Shows");

            log.Stop();
            Console.WriteLine($"Finished {This_Program} Program");
        }
    }
}