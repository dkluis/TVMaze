using Common_Lib;

using DB_Lib;

using DB_Lib_EF.Entities;
using DB_Lib_EF.Models.MariaDB;

using Entities_Lib;

namespace RefreshShows;

internal static class RefreshShows
{
    private static void Main()
    {
        const string thisProgram = "Refresh Shows";
        Console.WriteLine($"{DateTime.Now}: {thisProgram}");
        AppInfo appInfo = new("TVMaze", thisProgram, "DbAlternate");
        var     log     = appInfo.TxtFile;
        log.Start();
        LogModel.Start(thisProgram);

        try
        {
            using var db = new TvMaze();

            // Doing Skipping shows on Sunday Only
            if (DateTime.Now.ToString("ddd") == "Sun")
            {
                var skippingShows = db.Shows.Where(s => s.TvmStatus == "Skipping" && s.ShowStatus != "Ended") // && s.ShowStatus != "To Be Determined")
                                      .OrderByDescending(s => s.TvmShowId)
                                      .ToList();

                if (skippingShows != null)
                {
                    LogModel.Record(thisProgram, "Main", $"Starting Skipping Shows: {skippingShows.Count}");

                    foreach (var rec in skippingShows)
                    {
                        using ShowAndEpisodes sae = new(appInfo);
                        sae.Refresh(rec.TvmShowId);
                        log.Write($"Finished Refreshing Show {rec.ShowName}, {rec.TvmShowId}", "", 2);
                        LogModel.Record(thisProgram, "Main", $"Refreshing 'Skipping' show {rec.ShowName}, {rec.TvmShowId}", 4);
                    }

                    LogModel.Record(thisProgram, "Main", $"Finished Skipping Shows: {skippingShows.Count}");
                }
            }

            // Get all Shows to refresh today
            var response = ViewEntities.GetShowsToRefresh();

            if (response != null && !response.WasSuccess)
            {
                log.Write("Error occurred while getting shows to refresh", "Main", 1);
                LogModel.Record(thisProgram, "Main", $"Error Occurred Getting the base Shows To Refresh Information: {response.Message}", 6);

                // LogModel.Stop(thisProgram);
                // Environment.Exit(9);
            }

            var allShowsToRefreshInfo = (List<ViewEntities.ShowToRefresh>) response!.ResponseObject!;
            LogModel.Record(thisProgram, "Main", $"Found {allShowsToRefreshInfo.Count} total shows to refresh");

            var showsToday = allShowsToRefreshInfo.Where(s => s.TvmStatus != "Skipping").Take(300).ToList();
            LogModel.Record(thisProgram, "Main", $"Processing {showsToday.Count} shows in the 7 to 31 day range to refresh");

            foreach (var rec in showsToday)
            {
                using ShowAndEpisodes sae = new(appInfo);
                sae.Refresh(rec.TvmShowId);
                log.Write($"Refreshing Show not updated in 7 to 31 days  {rec.ShowName}, {rec.TvmShowId}", "", 2);
                LogModel.Record(thisProgram, "Main", $"Refreshing show not updated in 7 to 31 days {rec.ShowName}, {rec.TvmShowId}", 4);
            }

            // Get all shows to refresh that have episodes that without a broadcast date
            var showsNoBroadcastDate = db.Episodes.Where(e => e.BroadcastDate == null && e.PlexStatus == "").Select(e => e.TvmShowId).Distinct().ToList();
            LogModel.Record(thisProgram, "Main", $"Found {showsNoBroadcastDate.Count} shows having episodes with no broadcast date");

            foreach (var showId in showsNoBroadcastDate)
            {
                using ShowAndEpisodes sae = new(appInfo);
                sae.Refresh(showId);
                log.Write($"Refreshing Show that has episodes without a broadcast date {showId}", "", 2);
                LogModel.Record(thisProgram, "Main", $"Refreshing show {showId} that has episodes without a broadcast date", 4);
            }

            // Refresh all shows with Orphaned Episodes
            response = ViewEntities.GetEpisodesFullInfo(applyOrphanedFilter: true);

            if (response != null && response.WasSuccess && response.ResponseObject != null)
            {
                var showWithOrphanedEpisode = (List<int>) response.ResponseObject;
                LogModel.Record(thisProgram, "Main", $"Found {showWithOrphanedEpisode.Count} shows with orphaned episodes");

                foreach (var showId in showWithOrphanedEpisode)
                {
                    using ShowAndEpisodes sae = new(appInfo);
                    sae.Refresh(showId);
                    log.Write($"Refreshing Show with Orphaned episodes {showId}", "", 2);
                    LogModel.Record(thisProgram, "Main", $"Refreshing Show with Orphaned episodes {showId}", 4);
                }
            }

            // Get all Shows that will need to be acquired today to refresh
            response = ViewEntities.GetEpisodesToAcquire();

            if (response != null && response.WasSuccess && response.ResponseObject != null)
            {
                var showsToAcquire = (List<ViewEntities.ShowEpisode>) response.ResponseObject;
                LogModel.Record(thisProgram, "Main", $"Found {showsToAcquire.Count} shows with to acquire today");

                foreach (var rec in showsToAcquire)
                {
                    using ShowAndEpisodes sae = new(appInfo);
                    sae.Refresh(rec.TvmShowId);
                    log.Write($"Refreshing Show to acquire {rec.TvmShowId}", "", 2);
                    LogModel.Record(thisProgram, "Main", $"Refreshing Show to acquire {rec.TvmShowId}", 4);
                }
            }

            log.Stop();
            LogModel.Stop(thisProgram);
        }
        catch (Exception e)
        {
            LogModel.Record(thisProgram, "Main", $"Error Occured {e.Message} ::: {e.InnerException}", 20);
            log.Stop();
            LogModel.Stop(thisProgram);
        }
    }
}
