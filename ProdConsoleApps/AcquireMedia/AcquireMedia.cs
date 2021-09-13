using System;
using System.Diagnostics;

using Common_Lib;
using Web_Lib;
using Entities_Lib;
using MySqlConnector;

namespace AcquireMedia
{
    class Program
    {
        static void Main(string[] args)
        {
            string This_Program = "Acquire Media";
            Console.WriteLine($"{DateTime.Now}: {This_Program} Started");
            AppInfo appinfo = new("TVMaze", This_Program, "DbAlternate");
            Console.WriteLine($"{DateTime.Now}: {This_Program} Progress can be followed in {appinfo.FullPath}");
            TextFileHandler log = appinfo.TxtFile;
            log.Start();

            WebScrape webSrape = new(appinfo);
            Magnets media = new(appinfo);

            MySqlDataReader rdr;
            using (GetEpisodesToBeAcquired gea = new())
                rdr = gea.Find(appinfo);

            string magnet;
            while (rdr.Read())
            {
                magnet = media.PerformShowEpisodeMagnetsSearch(rdr["ShowName"].ToString(), int.Parse(rdr["Season"].ToString()), int.Parse(rdr["Episode"].ToString()), log);
                int episodeid = int.Parse(rdr["TvmEpisodeId"].ToString());

                log.EmptyLine();
                log.Write($"Found Magnet: {magnet}");
                log.EmptyLine();

                if (magnet == "") { continue; }

                using (Process AcquireMediaScript = new())
                {
                    AcquireMediaScript.StartInfo.FileName = "/Users/dick/TVMaze/Scripts/AcquireMediaViaTransmission.sh";
                    AcquireMediaScript.StartInfo.Arguments = magnet;
                    AcquireMediaScript.StartInfo.UseShellExecute = true;
                    AcquireMediaScript.StartInfo.RedirectStandardOutput = false;
                    bool started = AcquireMediaScript.Start();
                    AcquireMediaScript.WaitForExit();
                }

                using (Episode episode = new(appinfo))
                {
                    episode.FillViaTvmaze(episodeid);
                    episode.PlexStatus = "Acquired";
                    episode.PlexDate = DateTime.Now.ToString("yyyy-MM-dd");
                    episode.DbUpdate();
                }

                using (WebAPI wai = new(appinfo)) { wai.PutEpisodeToAcquired(episodeid); }
            }

            log.Stop();
            Console.WriteLine($"{DateTime.Now}: {This_Program} Finished");
        }
    }
}