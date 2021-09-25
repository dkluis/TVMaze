using System;
using Common_Lib;

namespace This_Program
{
    class Program
    {
        static void Main(string[] args)
        {
            string This_Program = "This Program";
            Console.WriteLine($"{DateTime.Now}: {This_Program}");
            AppInfo appinfo = new("TVMaze", This_Program, "DB Needed from Config");
            TextFileHandler log = appinfo.TxtFile;
            log.Start();



            log.Stop();
        }
    }
}