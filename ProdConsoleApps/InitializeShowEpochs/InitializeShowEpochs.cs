using System;
using Common_Lib;
using DB_Lib;
using Web_Lib;

namespace InitializeShowEpochs;

internal class InitializeShowEpochs
{
    private static void Main()
    {
        var thisProgram = "Init Show Epochs";
        Console.WriteLine($"{DateTime.Now}: {thisProgram}");
        AppInfo appinfo = new("TVMaze", thisProgram, "DbAlternate");
        var log = appinfo.TxtFile;
        log.Start();

        WebApi tvmapi = new(appinfo);
        var jsoncontent = tvmapi.ConvertHttpToJArray(tvmapi.GetAllEpisodes());
        Console.WriteLine(jsoncontent.ToString());

        /*var iuIdx = 0;
        MariaDb mdbw = new(appinfo);

        foreach (var show in jsoncontent)
        {
            if (show["show_id"] is not null)
            {
                var showid = int.Parse(show["show_id"].ToString());
                log.Write($"insert into TvmShowUpdates values (0, {showid}, 0, '{DateTime.Now:yyyy-MM-dd}');", "", 4);
                var rows = mdbw.ExecNonQuery($"insert into TvmShowUpdates values (0, {showid}, 1, '{DateTime.Now:yyyy-MM-dd}');");
                if (rows == 0) log.Write($"Insert went wrong for {showid}");
            }
            else
            {
                log.Write("JToken Show[id] was null", "", 0);
            }

            iuIdx++;
        }

        log.Write($"Processed {iuIdx} Followed Shows");

        log.Stop();*/
    }
}
