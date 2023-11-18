using System.Diagnostics;
using Common_Lib;
using DB_Lib;
using Entities_Lib;
using OpenQA.Selenium.Chrome;
using Web_Lib;

namespace AcquireMedia;

internal static class Program
{
    private static void Main()
    {
        const string thisProgram = "Acquire Media";
        Console.WriteLine($"{DateTime.Now}: {thisProgram}");
        AppInfo appInfo = new("TVMaze", thisProgram, "DbAlternate");
        var log = appInfo.TxtFile;
        log.Start();

        Magnets media = new(appInfo);
        using GetEpisodesToBeAcquired gea = new();
        var rdr = gea.Find(appInfo);

        log.Write("Starting Chrome Selenium Driver", thisProgram, 4);
        var options = new ChromeOptions();
        options.AddArgument("--headless");
        var browserDriver = new ChromeDriver(options);

        var isSeason = false;
        var showId = 0;
        while (rdr.Read())
        {
            if (isSeason && showId == int.Parse(rdr["TvmShowId"].ToString()!)) continue;
            showId = 0;
            var showName = rdr["AltShowName"].ToString() != ""
                ? rdr["AltShowName"].ToString()!.Replace("(", "").Replace(")", "")
                : rdr["ShowName"].ToString()!;
            var episodeId = int.Parse(rdr["TvmEpisodeId"].ToString()!);
            var (seasonInd, magnet) = media.PerformShowEpisodeMagnetsSearch(showName,
                int.Parse(rdr["Season"].ToString()!),
                int.Parse(rdr["Episode"].ToString()!),
                log,
                browserDriver);
            isSeason = seasonInd;

            if (magnet == "")
            {
                log.Write($"Magnet Not Found for {rdr["ShowName"]}, {rdr["Season"]}-{rdr["Episode"]}");
                continue;
            }

            var temp = magnet.Split("tr=");
            log.Write(
                $"Found Magnet for {rdr["ShowName"]}, {rdr["Season"]}-{rdr["Episode"]} " +
                $"Processing Whole Season is {isSeason}: {temp[0]}");

            using (Process acquireMediaScript = new())
            {
                acquireMediaScript.StartInfo.FileName = "/Users/dick/TVMaze/Scripts/AcquireMediaViaTransmission.sh";
                acquireMediaScript.StartInfo.Arguments = magnet;
                acquireMediaScript.StartInfo.UseShellExecute = true;
                acquireMediaScript.StartInfo.RedirectStandardOutput = false;
                var result = acquireMediaScript.Start();
                acquireMediaScript.WaitForExit();
            }

            if (!isSeason)
            {
                using Episode episode = new(appInfo);
                episode.FillViaTvmaze(episodeId);
                episode.PlexStatus = "Acquired";
                episode.PlexDate = DateTime.Now.ToString("yyyy-MM-dd");
                episode.DbUpdate();
                using WebApi wai = new(appInfo);
                wai.PutEpisodeToAcquired(episodeId);
            }
            else
            {
                showId = int.Parse(rdr["TvmShowId"].ToString()!);
                var season = int.Parse(rdr["Season"].ToString()!);
                using MariaDb mdb = new(appInfo);
                var seasRdr =
                    mdb.ExecQuery(
                        $"select * from Episodes where `TvmShowId` = {showId} and `Season` = {season}");
                while (seasRdr.Read())
                {
                    var seasEpiId = int.Parse(seasRdr["TvmEpisodeId"].ToString()!);
                    using Episode seasEpi = new(appInfo);
                    seasEpi.FillViaTvmaze(seasEpiId);
                    seasEpi.PlexStatus = "Acquired";
                    seasEpi.PlexDate = DateTime.Now.ToString("yyyy-MM-dd");
                    seasEpi.DbUpdate();
                    using WebApi wai = new(appInfo);
                    wai.PutEpisodeToAcquired(seasEpiId);
                }
            }
        }

        log.Write("Quiting Chrome Selenium Driver", thisProgram, 4);
        browserDriver.Quit();
        log.Stop();
    }
}