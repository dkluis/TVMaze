using System;
using Common_Lib;

namespace This_Program
{
    class Program
    {
        static void Main(string[] args)
        {
            string This_Program = "This Program";
            Console.WriteLine($"{DateTime.Now}: Starting {This_Program}");
            AppInfo appinfo = new("TVMaze", This_Program, "DB Needed from Config");
            Console.WriteLine($"{DateTime.Now}: Progress can be followed in {appinfo.FullPath}");
            TextFileHandler log = appinfo.TxtFile;
            log.Start();



            log.Stop();
            Console.WriteLine($"{DateTime.Now}: Finished {This_Program} Program");
        }
    }
}