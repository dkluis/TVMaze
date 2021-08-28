using System;
using Common_Lib;
using Entities_Lib;

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

            using MediaFileHandler mfh = new(appinfo);
            {
                List<string> files = mfh.GetMediaByShow("TS", "The Killer Truth", 1, 1);
                log.Write($"{files.Count}");
                foreach (string file in files)
                {
                    log.Write($"{file}");
                }
            }


            log.Stop();
            Console.WriteLine($"{DateTime.Now}: {This_Program} Finished");
        }
    }
}