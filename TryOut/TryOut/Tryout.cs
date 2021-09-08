using System;
using Common_Lib;
using Entities_Lib;
using Web_Lib;

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

            // Refresh a single show ///////////////
            
            using (ShowAndEpisodes sae = new(appinfo))
            {
                int showid = 51121;
                log.Write($"Working on Refreshing Show {showid}", "", 2);
                sae.Refresh(showid);
            }
            

            //using (WebAPI uts = new(appinfo)) { HttpResponseMessage hs = uts.PutEpisodeToAcquired(2154680); }
            /*
            using (Process curl = new())
            {
                curl.StartInfo.FileName = "/Users/dick/TVMaze/Scripts/tvm_curl.sh - d '{episode_id:27,\"marked_at\":0,\"type\": 2}' 'https://api.tvmaze.com/v1/user/episodes/2154680'";
                curl.StartInfo.Arguments = ("-d '{episode_id:27,\"marked_at\":0,\"type\": 2}' 'https://api.tvmaze.com/v1/user/episodes/2154680'");
                curl.StartInfo.UseShellExecute = true;
                curl.Start();
                curl.WaitForExit();
                //Console.ReadLine();
            }
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