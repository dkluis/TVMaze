using System.Diagnostics;

using Common_Lib;

using DB_Lib;

using DB_Lib_EF.Entities;

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
        var     log     = appInfo.TxtFile;
        log.Start();
        LogModel.Start(thisProgram);

        try
        {
            Magnets                       media = new(appInfo);
            using GetEpisodesToBeAcquired gea   = new();
            var                           rdr   = gea.Find(appInfo);

            var isSeason = false;
            var showId   = 0;
            bool isSeleniumStarted;

            while (rdr.Read())
            {
                log.Write("Starting Chrome Selenium Driver", thisProgram, 4);
                LogModel.Record(thisProgram, "Main", "Starting Chrome Selenium Driver", 4);
                var options = new ChromeOptions();
                options.AddArgument("--headless");
                options.AddArgument("--whitelisted-ips=''");
                options.AddArgument("--disable-dev-shm-usage");
                options.AddArgument("--disable-popup-blocking");
                options.AcceptInsecureCertificates = true;
                var browserDriver = new ChromeDriver(options);
                isSeleniumStarted = true;

                if (isSeason && showId == int.Parse(rdr["TvmShowId"].ToString()!)) continue;
                showId = 0;
                var showName  = rdr["AltShowName"].ToString() != "" ? rdr["AltShowName"].ToString()!.Replace("(", "").Replace(")", "") : rdr["ShowName"].ToString()!;
                var episodeId = int.Parse(rdr["TvmEpisodeId"].ToString()!);
                var (seasonInd, magnet) = media.PerformShowEpisodeMagnetsSearch(showName, int.Parse(rdr["Season"].ToString()!), int.Parse(rdr["Episode"].ToString()!), log, browserDriver);
                isSeason                = seasonInd;

                if (magnet == "")
                {
                    log.Write($"Magnet Not Found for {rdr["ShowName"]}, {rdr["Season"]}-{rdr["Episode"]}");
                    LogModel.Record(thisProgram, "Main", $"Magnet Not Found for {rdr["ShowName"]}, {rdr["Season"]}-{rdr["Episode"]}", 4);

                    if (isSeleniumStarted)
                    {
                        LogModel.Record(thisProgram, "Main", $"Quiting Chrome Selenium Driver", 4);
                        browserDriver.Quit();
                    }

                    continue;
                }

                var temp = magnet.Split("tr=");
                log.Write($"Found Magnet for {rdr["ShowName"]}, {rdr["Season"]}-{rdr["Episode"]} "                            + $"Processing Whole Season is {isSeason}: {temp[0]}");
                LogModel.Record(thisProgram, "Main", $"Found Magnet for {rdr["ShowName"]}, {rdr["Season"]}-{rdr["Episode"]} " + $"Processing Whole Season is {isSeason}: {temp[0]}", 4);

                using (Process acquireMediaScript = new())
                {
                    acquireMediaScript.StartInfo.FileName               = "/media/psf/TVMazeLinux/Scripts/TorrentToTransmission.sh";
                    acquireMediaScript.StartInfo.Arguments              = magnet;
                    acquireMediaScript.StartInfo.UseShellExecute        = true;
                    acquireMediaScript.StartInfo.RedirectStandardOutput = false;
                    var result = acquireMediaScript.Start();
                    acquireMediaScript.WaitForExit();
                    LogModel.Record(thisProgram, "Main", $"Transferred magnet to Transmission {magnet}", 3);
                }

                if (!isSeason)
                {
                    using Episode episode = new(appInfo);
                    episode.FillViaTvmaze(episodeId);
                    episode.PlexStatus = "Acquired";
                    episode.PlexDate   = DateTime.Now.ToString("yyyy-MM-dd");
                    episode.DbUpdate();
                    using WebApi wai = new(appInfo);
                    wai.PutEpisodeToAcquired(episodeId);
                } else
                {
                    showId = int.Parse(rdr["TvmShowId"].ToString()!);
                    var           season  = int.Parse(rdr["Season"].ToString()!);
                    using MariaDb mdb     = new(appInfo);
                    var           seasRdr = mdb.ExecQuery($"select * from Episodes where `TvmShowId` = {showId} and `Season` = {season}");

                    while (seasRdr.Read())
                    {
                        var           seasEpiId = int.Parse(seasRdr["TvmEpisodeId"].ToString()!);
                        using Episode seasEpi   = new(appInfo);
                        seasEpi.FillViaTvmaze(seasEpiId);
                        seasEpi.PlexStatus = "Acquired";
                        seasEpi.PlexDate   = DateTime.Now.ToString("yyyy-MM-dd");
                        seasEpi.DbUpdate();
                        using WebApi wai = new(appInfo);
                        wai.PutEpisodeToAcquired(seasEpiId);
                    }
                }

                if (isSeleniumStarted)
                {
                    LogModel.Record(thisProgram, "Main", $"Quiting Chrome Selenium Driver", 4);
                    isSeleniumStarted = false;
                    browserDriver.Quit();
                }
            }
        }
        catch (Exception e)
        {
            LogModel.Record(thisProgram, "Main", $"Exception Occurred {e.Message}", 6);

            throw;
        }

        log.Stop();
        LogModel.Stop(thisProgram);
    }
}
