using System;
using Common_Lib;
using Newtonsoft.Json.Linq;


namespace QuickTests
{
    class Program
    {
        static void Main()
        {
            string[] newpath = new string[] { "Users", "Dick", "TVMaze", "Logs" };
            AppInfo appinfo = new("QuickTest", "File Read", "QuickTest.log", newpath);
            TextFileHandler log = appinfo.TxtFile;
            log.Start();

            AppInfo file = new("QuickTest", "File Read", "QuickTest.txt", newpath);
            TextFileHandler config = file.TxtFile;
            config.WriteNoHead("[{", true, false);
            config.WriteNoHead("\"First Record\": \"Some Info\",");
            config.WriteNoHead("\"Second Record\": \"Some Info\"");
            config.WriteNoHead("}]");

            log.Write("Created the config file");
            log.Elapsed();

            string fileinfo = config.ReadFile();

            JArray kvps = ConvertJsonTxt.ConvertStringToJArray(fileinfo);
            foreach (var kvp in kvps)
            {
                log.Write($"KVP is: {kvp}");
                log.Write($"{kvp["First Record"]}");
            }

            log.Start();
        }
    }
}
