using System.Diagnostics;

using Common_Lib;

using DB_Lib_EF.Entities;
using DB_Lib_EF.Models.MariaDB;

using OpenQA.Selenium.Chrome;

using Web_Lib;

using Episode = Entities_Lib.Episode;

namespace AcquireMedia;

internal static class Program
{
    private static void Main()
    {
        const string thisProgram = "Acquire Media";
        AppInfo appInfo = new("TVMaze", thisProgram, "DbAlternate");
        var     log     = appInfo.TxtFile;
        LogModel.Start(thisProgram);

        try
        {
            Magnets media    = new(appInfo);
            var     response = ViewEntities.GetEpisodesToAcquire();

            if (response != null && response.WasSuccess && response.ResponseObject != null)
            {
                var episodesToBeAcquired = (List<ViewEntities.ShowEpisode>) response.ResponseObject;

                var  isSeason = false;
                var  showId   = 0;
                bool isSeleniumStarted;

                foreach (var rec in episodesToBeAcquired)
                {
                    LogModel.Record(thisProgram, "Main", $"Processing: {rec.ShowName}, {rec.SeasonEpisode}", 1);
                    log.Write("Starting Chrome Selenium Driver", thisProgram, 4);
                    LogModel.Record(thisProgram, "Main", "Starting Chrome Selenium Driver", 5);
                    var options = new ChromeOptions();
                    options.AddArgument("--headless");
                    options.AddArgument("--whitelisted-ips=''");
                    options.AddArgument("--disable-dev-shm-usage");
                    options.AddArgument("--disable-popup-blocking");
                    options.AcceptInsecureCertificates = true;
                    var browserDriver = new ChromeDriver(options);
                    isSeleniumStarted = true;

                    if (isSeason && showId == rec.TvmShowId) continue;

                    showId = 0;
                    var showName  = rec.AltShowName != "" ? rec.AltShowName!.Replace("(", "").Replace(")", "") : rec.ShowName;
                    var episodeId = rec.TvmEpisodeId;
                    var (seasonInd, magnet) = media.PerformShowEpisodeMagnetsSearch(showName!, rec.Season, rec.Episode, browserDriver);
                    isSeason                = seasonInd;

                    if (magnet == "")
                    {
                        log.Write($"Magnet Not Found for {rec.ShowName}, {rec.SeasonEpisode}");
                        LogModel.Record(thisProgram, "Main", $"Magnet Not Found for {rec.ShowName}, {rec.SeasonEpisode}", 1);

                        if (isSeleniumStarted)
                        {
                            LogModel.Record(thisProgram, "Main", $"Quiting Chrome Selenium Driver", 5);
                            browserDriver.Quit();
                        }

                        continue;
                    }

                    var temp = magnet.Split("tr=");
                    log.Write($"Found Magnet for {rec.ShowName}, {rec.SeasonEpisode} "                            + $"Processing Whole Season is {isSeason}: {temp[0]}");
                    LogModel.Record(thisProgram, "Main", $"Found Magnet for {rec.ShowName}, {rec.SeasonEpisode} " + $"Processing Whole Season is {isSeason}: {temp[0]}", 4);

                    using (Process acquireMediaScript = new())
                    {
                        acquireMediaScript.StartInfo.FileName               = "/media/psf/TVMazeLinux/Scripts/TorrentToTransmission.sh";
                        acquireMediaScript.StartInfo.Arguments              = magnet;
                        acquireMediaScript.StartInfo.UseShellExecute        = true;
                        acquireMediaScript.StartInfo.RedirectStandardOutput = false;
                        var result = acquireMediaScript.Start();
                        acquireMediaScript.WaitForExit();
                        LogModel.Record(thisProgram, "Main", $"Transferred magnet to Transmission {magnet}",                    2);
                        LogModel.Record(thisProgram, "Main", $"Transmission is loading {rec.SeasonEpisode} for {rec.ShowName}", 1);
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
                        using var db = new TvMaze();

                        var episodes = db.Episodes.Where(e => e.TvmShowId == rec.TvmShowId && e.Season == rec.Season).Select(e => e.TvmEpisodeId).ToList();

                        foreach (var seasEpiId in episodes)
                        {
                            using Episode seasEpi = new(appInfo);
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
                        LogModel.Record(thisProgram, "Main", $"Quiting Chrome Selenium Driver", 5);
                        isSeleniumStarted = false;
                        browserDriver.Quit();
                    }
                }
            } else
            {
                LogModel.Record(thisProgram, "Main", $"Nothing was returned from ViewItem.EpisodesToAcquire", 1);
            }
        }
        catch (Exception e)
        {
            LogModel.Record(thisProgram, "Main", $"Exception Occurred {e.Message}  ::: {e.InnerException}", 20);
        }

        LogModel.Stop(thisProgram);
    }
}
