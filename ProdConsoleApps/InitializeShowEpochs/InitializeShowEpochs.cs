using System;
using Common_Lib;
using DB_Lib;
using Web_Lib;

namespace InitializeShowEpochs
{
    internal class InitializeShowEpochs
    {
        private static void Main()
        {
            var This_Program = "Init Show Epochs";
            Console.WriteLine($"{DateTime.Now}: {This_Program}");
            AppInfo appinfo = new("TVMaze", This_Program, "DbAlternate");
            var log = appinfo.TxtFile;
            log.Start();

            WebAPI tvmapi = new(appinfo);
            var jsoncontent = tvmapi.ConvertHttpToJArray(tvmapi.GetFollowedShows());

            var iu_idx = 0;
            MariaDB Mdbw = new(appinfo);

            foreach (var show in jsoncontent)
            {
                if (show["show_id"] is not null)
                {
                    var showid = int.Parse(show["show_id"].ToString());
                    log.Write($"insert into TvmShowUpdates values (0, {showid}, 0, '{DateTime.Now:yyyy-MM-dd}');", "",
                        4);
                    var rows = Mdbw.ExecNonQuery(
                        $"insert into TvmShowUpdates values (0, {showid}, 1, '{DateTime.Now:yyyy-MM-dd}');");
                    if (rows == 0) log.Write($"Insert went wrong for {showid}");
                }
                else
                {
                    log.Write("JToken Show[id] was null", "", 0);
                }

                iu_idx++;
            }

            log.Write($"Processed {iu_idx} Followed Shows");

            log.Stop();
        }
    }
}