using System;
using Common_Lib;
using Entities_Lib;

namespace InitializeEpisodes
{
    class InitializeEpisodes
    {
        static void Main()
        {
            string This_Program = "Init Episode Table";
            Console.WriteLine($"{DateTime.Now}: Starting {This_Program}");
            AppInfo appinfo = new("TVMaze", This_Program, "DbAlternate");
            Console.WriteLine($"{DateTime.Now}: Progress can be followed in {appinfo.FullPath}");
            TextFileHandler log = appinfo.TxtFile;
            log.Start();

            Episode epi = new(appinfo);

            epi.FillViaTvmaze(1);
            log.Write($"{epi.ShowName} {epi.TvmImage}, {epi.TvmRunTime}");
            epi.DbInsert();
            epi.Reset();
            epi.FillViaTvmaze(2);
            epi.DbInsert();
            epi.Reset();
            log.Write($"{epi.ShowName} {epi.TvmImage}, {epi.TvmRunTime}");

            log.Stop();
            Console.WriteLine($"{DateTime.Now}: Finished {This_Program} Program");
        }
    }
}