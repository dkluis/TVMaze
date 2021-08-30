using System;
using Common_Lib;
using Entities_Lib;
using Web_Lib;

using System.Collections.Generic;

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

            WebAPI wa = new(appinfo);
            wa.PutEpisodeToWatched(2148111);

            Episode epi = new(appinfo);
            epi.FillViaTvmaze(2148111);
            
            
            


            log.Stop();
            Console.WriteLine($"{DateTime.Now}: {This_Program} Finished");
        }
    }
}