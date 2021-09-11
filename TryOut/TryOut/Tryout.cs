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



            using (ShowAndEpisodes sae = new(appinfo))
            {
                int showid = 57345;
                log.Write($"Working on Refreshing Show {showid}", "", 2);
                sae.Refresh(showid);
            }

            /*
            WebAPI showRss = new(appinfo);
            HttpClientHandler hch = showRss.ShowRssLogin("user", "password");
            */

            /*
            using (Process python = new())
            {
                python.StartInfo.FileName = "/Volumes/HD-Data-CA-Server/PlexMedia/PlexProcessing/TVMaze/Scripts/tvmaze.sh";
                python.StartInfo.UseShellExecute = true;
                python.StartInfo.RedirectStandardOutput = false;
                bool started = python.Start();
                python.WaitForExit();
            }
            */

            log.Stop();
            Console.WriteLine($"{DateTime.Now}: {This_Program} Finished");
        }
    }
}