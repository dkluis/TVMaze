using System.Diagnostics;
using Common_Lib;
using DB_Lib_EF.Entities;
using DB_Lib_EF.Models.MariaDB;
using Web_Lib;
using Episode = Entities_Lib.Episode;

namespace AcquireMedia;

internal static class Program
{
    private static void Main()
    {
        const string thisProgram = "Acquire Media";
        AppInfo appInfo = new("TVMaze", thisProgram, "DbAlternate");
        if (!LogModel.IsSystemActive())
        {
            LogModel.InActive(thisProgram);
            Environment.Exit(99);
        }
        LogModel.Start(thisProgram);

        try
        {
            Magnets media = new(thisProgram);
            var response = ViewEntities.GetEpisodesToAcquire();
            if (response is {WasSuccess: true, ResponseObject: not null})
            {
                var episodesToBeAcquired = (List<ViewEntities.ShowEpisode>) response.ResponseObject;
                var isSeason = false;
                var showId = -99;

                foreach (var rec in episodesToBeAcquired)
                {
                    if (isSeason && showId == rec.TvmShowId) { continue; }
                    showId = rec.TvmShowId;
                    LogModel.Record(thisProgram, "Main", $"Processing: {rec.ShowName}, {rec.SeasonEpisode}");
                    LogModel.Record(thisProgram, "Main", "Stopping & Starting Chrome Selenium Driver", 3);
                    var showName = rec.AltShowName != "" ? rec.AltShowName!.Replace("(", "").Replace(")", "") : rec.ShowName;
                    var episodeId = rec.TvmEpisodeId;
                    var (seasonInd, magnet) = media.PerformShowEpisodeMagnetsSearch(showName!, rec.Season, rec.Episode);
                    isSeason = seasonInd;

                    if (magnet == "")
                    {
                        LogModel.Record(thisProgram, "Main", $"Magnet Not Found for {rec.ShowName}, {rec.SeasonEpisode}");
                        continue;
                    }

                    var temp = magnet.Split("tr=");
                    LogModel.Record(thisProgram, "Main", $"Found Magnet for {rec.ShowName}, {rec.SeasonEpisode} " + $"Processing Whole Season is {isSeason}: {temp[0]}", 4);

                    using (Process acquireMediaScript = new())
                    {
                        acquireMediaScript.StartInfo.FileName = "/media/psf/TVMazeLinux/Scripts/TorrentToTransmission.sh";
                        acquireMediaScript.StartInfo.Arguments = magnet;
                        acquireMediaScript.StartInfo.UseShellExecute = true;
                        acquireMediaScript.StartInfo.RedirectStandardOutput = false;
                        acquireMediaScript.Start();
                        acquireMediaScript.WaitForExit();
                        LogModel.Record(thisProgram, "Main", $"Transferred magnet to Transmission {magnet}", 2);
                        LogModel.Record(thisProgram, "Main", $"Transmission is loading {rec.SeasonEpisode} for {rec.ShowName}");
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
                    } else
                    {
                        using var db = new TvMaze();
                        var episodes = db.Episodes.Where(e => e.TvmShowId == rec.TvmShowId && e.Season == rec.Season).Select(e => e.TvmEpisodeId).ToList();
                        foreach (var seasEpiId in episodes)
                        {
                            using Episode seasEpi = new(appInfo);
                            seasEpi.FillViaTvmaze(seasEpiId);
                            seasEpi.PlexStatus = "Acquired";
                            seasEpi.PlexDate = DateTime.Now.ToString("yyyy-MM-dd");
                            seasEpi.DbUpdate();
                            using WebApi wai = new(appInfo);
                            wai.PutEpisodeToAcquired(seasEpiId);
                        }
                    }
                    break;
                }
            } else
            {
                LogModel.Record(thisProgram, "Main", "Nothing was returned from ViewItem.EpisodesToAcquire");
            }
        }
        catch (Exception e)
        {
            LogModel.Record(thisProgram, "Main", $"Exception Occurred {e.Message}  ::: {e.InnerException}", 20);
        }

        LogModel.Stop(thisProgram);
    }
}
