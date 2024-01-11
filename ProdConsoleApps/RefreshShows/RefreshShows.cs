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

        MariaDb   mDbR = new(appInfo);
        using var db   = new TvMaze();

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
            LogModel.Stop(thisProgram);
            Environment.Exit(9);
        }

        var allShowsToRefreshInfo = (List<ViewEntities.ShowToRefresh>) response!.ResponseObject!;

        if (allShowsToRefreshInfo.Count > 0)
        {
            LogModel.Record(thisProgram, "Main", $"Found {allShowsToRefreshInfo.Count} shows to refresh");

            var showsToday = allShowsToRefreshInfo.Where(s => s.TvmStatus != "Skipping").Take(300).ToList();
            LogModel.Record(thisProgram, "Main", $"Found {showsToday.Count} shows in the 7 to 31 day range to refresh");

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
                log.Write($"Refreshing Show not updated in 7 to 31 days {showId}", "", 2);
                LogModel.Record(thisProgram, "Main", $"Refreshing show {showId} that has episodes without a broadcast date", 4);
            }

            // Refresh all shows with Orphaned Episodes
            var showWithOrphanedEpisode = ViewEntities.GetEpisodesFullInfo(applyOrphanedFilter: true);
            var rdr                     = mDbR.ExecQuery("select distinct `TvmShowId` from OrphanedEpisodes order by `TvmShowId`;");

            while (rdr.Read())
            {
                using ShowAndEpisodes sae = new(appInfo);
                log.Write($"Working on Epi Update for Orphaned Episodes {rdr[0]}", "", 2);
                sae.Refresh(int.Parse(rdr[0].ToString()!));

                //Thread.Sleep(1000);
            }

            mDbR.Close();
        } else
        {
            LogModel.Record(thisProgram, "Main", "No Shows found to Refresh");
        }

        // Get all Shows that will need to be acquired today to refresh
        var rdr1 = mDbR.ExecQuery("select distinct `TvmShowId` from EpisodesFromTodayBack order by `TvmShowId` desc;");

        while (rdr1.Read())
        {
            using ShowAndEpisodes sae = new(appInfo);
            log.Write($"Working on Today's Show {rdr1[0]}", "", 2);
            sae.Refresh(int.Parse(rdr1[0].ToString()!));

            //Thread.Sleep(1000);
        }

        mDbR.Close();

        log.Stop();
        LogModel.Stop(thisProgram);
    }
}
