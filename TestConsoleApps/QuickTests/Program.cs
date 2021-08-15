using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Common_Lib;
using Web_Lib;
using HtmlAgilityPack;

namespace QuickTests
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Started QuickTests");
            AppInfo appinfo = new("TVMaze", "QuickTest" , "DbProduction");
            TextFileHandler log = appinfo.TxtFile;
            TextFileHandler cnf = appinfo.CnfFile;
            log.Start();


            //log.Write($"{appinfo.DbProdConn}");
            //log.Write($"{appinfo.ConfigFullPath}");
            //log.Write($"{appinfo.TvmazeToken}");
            //log.Write($"{appinfo.LogLevel}");
            //log.Write($"{appinfo.ActiveDBConn}");
            log.Write($"{appinfo.HomeDir}");
            log.Write($"{appinfo.TvmazeToken}");
            log.Write($"{appinfo.RarbgToken}");
            //log.Write($"{appinfo.FullPath}");


            ReadKeyFromFile rkffo = new();
            //log.Write($"Direct Object Read: {rkffo.FindInArray(appinfo.ConfigFullPath, "LogLevel")}");
            log.Write($"Direct Object Read: {rkffo.FindInArray(appinfo.ConfigFullPath, "DbAlternate")}");
            //log.Write($"Direct Object Read: {rkffo.FindInArray(appinfo.ConfigFullPath, "SSS")}");


            //System.Diagnostics.Process.Start(@"/Applications/Safari.app/Contents/MacOS/Safari");

            WebScrape showscrape = new(appinfo);
            List<string> ShowRssShows = showscrape.GetShowRssInfo();
            log.Write($"Found {ShowRssShows.Count} in the ShowRss HTML download");
            foreach (string show in ShowRssShows)
            {
                log.Write($"Found show: ---{show}---");
            }
            Console.WriteLine("Finished QuickTests");
            log.Stop();
        }
    }
}
