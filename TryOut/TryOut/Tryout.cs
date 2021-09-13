using System;
using Common_Lib;
using Entities_Lib;
using Web_Lib;
using DB_Lib;

using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Diagnostics;
using System.Threading;

namespace TryOut
{
    class TryingOut
    {
        static void Main()
        {
            string This_Program = "Trying Out";
            Console.WriteLine($"{DateTime.Now}: {This_Program} Started");
            AppInfo appinfo = new("TVMaze", This_Program, "DbAlternate");
            Console.WriteLine($"{DateTime.Now}: {This_Program} Progress can be followed in {appinfo.FullPath}");
            TextFileHandler log = appinfo.TxtFile;
            log.Start();


            /*
            using (ShowAndEpisodes sae = new(appinfo))
            {
                int showid = 56906;
                log.Write($"Working on Refreshing Show {showid}", "", 2);
                sae.Refresh(showid);
            }
            */

            string name = "kung fu (2018)";
            Console.WriteLine(Common.RemoveSuffixFromShowname(name));
            name = "kung fu 2018";
            Console.WriteLine(Common.RemoveSuffixFromShowname(name));
            name = "kung fu (us)";
            Console.WriteLine(Common.RemoveSuffixFromShowname(name));


            /*
            WebAPI showRss = new(appinfo);
            HttpClientHandler hch = showRss.ShowRssLogin("user", "password");
            */

            log.Stop();
            Console.WriteLine($"{DateTime.Now}: {This_Program} Finished");
        }
    }
}