using System;
using Common_Lib;

namespace UpdateShowEpochs
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting UpdateShowEpochs");
            AppInfo appinfo = new("TVMaze", "Update Show Epochs", "DbAlternate");
            TextFileHandler log = appinfo.TxtFile;
            log.Start();



            log.Stop();
            Console.WriteLine("Finishing UpdateShowEpochs");
        }
    }
}