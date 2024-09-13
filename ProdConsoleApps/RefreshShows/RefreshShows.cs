using Common_Lib;
using DB_Lib_EF.Entities;
using DB_Lib_EF.Models.MariaDB;
using Entities_Lib;

namespace RefreshShows;

internal static class RefreshShows
{
    private static void Main()
    {
        const string thisProgram = "Refresh Shows";
        AppInfo      appInfo     = new("TVMaze", thisProgram, "DbAlternate");
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
                LogModel.Record(thisProgram, "Main", $"Starting Skipping Shows: {skippingShows.Count}");
                foreach (var rec in skippingShows)
                {
                    using ShowAndEpisodes sae = new(appInfo);
                    sae.Refresh(rec.TvmShowId);
                    LogModel.Record(thisProgram, "Main", $"Refreshing 'Skipping' show {rec.ShowName}, {rec.TvmShowId}", 4);
                }
                LogModel.Record(thisProgram, "Main", $"Finished Skipping Shows: {skippingShows.Count}");

                // Doing the Review shows as well
                var reviewShows = db.Shows.Where(r => r.TvmStatus == "Reviweing").OrderByDescending(r => r.TvmShowId).ToList();

            }

            // Get all Shows to refresh today
            var response = ViewEntities.GetShowsToRefresh();

            if (!response.WasSuccess) LogModel.Record(thisProgram, "Main", $"Error Occurred Getting the base Shows To Refresh Information: {response.Message}", 6);

            var allShowsToRefreshInfo = (List<ViewEntities.ShowToRefresh>) response.ResponseObject!;

            //LogModel.Record(thisProgram, "Main", $"Found {allShowsToRefreshInfo.Count} total shows to refresh");

            var showsToday = allShowsToRefreshInfo.Where(s => s.TvmStatus != "Skipping").Take(300).ToList();
            LogModel.Record(thisProgram, "Main", $"Processing {showsToday.Count} shows in the 7 to 31 day range to refresh");

            foreach (var rec in showsToday)
            {
                using ShowAndEpisodes sae = new(appInfo);
                sae.Refresh(rec.TvmShowId);
                LogModel.Record(thisProgram, "Main", $"Refreshing show not updated in 7 to 31 days {rec.ShowName}, {rec.TvmShowId}", 2);
            }

            // Get all shows to refresh that have episodes that without a broadcast date
            var showsNoBroadcastDate = db.Episodes.Where(e => e.BroadcastDate == null && e.PlexStatus == "").Select(e => e.TvmShowId).Distinct().ToList();
            LogModel.Record(thisProgram, "Main", $"Found {showsNoBroadcastDate.Count} shows having episodes with no broadcast date");

            foreach (var showId in showsNoBroadcastDate)
            {
                using ShowAndEpisodes sae = new(appInfo);
                sae.Refresh(showId);
                LogModel.Record(thisProgram, "Main", $"Refreshing show {showId} that has episodes without a broadcast date", 2);
            }

            // Refresh all shows with Orphaned Episodes
            response = ViewEntities.GetEpisodesFullInfo(true);

            if (response is {WasSuccess: true, ResponseObject: not null})
            {
                var showWithOrphanedEpisode = (List<int>) response.ResponseObject;
                LogModel.Record(thisProgram, "Main", $"Found {showWithOrphanedEpisode.Count} shows with orphaned episodes");

                foreach (var showId in showWithOrphanedEpisode)
                {
                    using ShowAndEpisodes sae = new(appInfo);
                    sae.Refresh(showId);
                    LogModel.Record(thisProgram, "Main", $"Refreshing Show with Orphaned episodes {showId}", 2);
                }
            }

            // Get all Shows that will need to be acquired today to refresh
            response = ViewEntities.GetEpisodesToAcquire();

            if (response is {WasSuccess: true, ResponseObject: not null})
            {
                var episodesToAcquire = (List<ViewEntities.ShowEpisode>) response.ResponseObject;
                var showsToAcquire    = episodesToAcquire.OrderBy(e => e.TvmShowId).DistinctBy(e => e.TvmShowId).ToList();
                LogModel.Record(thisProgram, "Main", $"Found {showsToAcquire.Count} shows with to acquire today");

                foreach (var rec in showsToAcquire)
                {
                    using ShowAndEpisodes sae = new(appInfo);
                    sae.Refresh(rec.TvmShowId);
                    LogModel.Record(thisProgram, "Main", $"Refreshing Show to acquire {rec.TvmShowId}", 2);
                }
            }

            LogModel.Stop(thisProgram);
        }
        catch (Exception e)
        {
            LogModel.Record(thisProgram, "Main", $"Error Occured {e.Message} ::: {e.InnerException}", 20);
            LogModel.Stop(thisProgram);
        }
    }
}
