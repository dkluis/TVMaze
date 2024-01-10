using System;
using System.Collections.Generic;

using Common_Lib;

using DB_Lib;

using DB_Lib_EF.Entities;

using Entities_Lib;

using Web_Lib;

namespace UpdateFollowed;

/// <summary>
///     1. Gets all Followed marked shows from Tvmaze Web
///     2. All Tvmaze Web show are evaluated and updated or inserted based on what is in Tvmaze Local and are marked as
///     Following
///     3. Deletes all Shows that were followed before but have been unfollowed
/// </summary>
internal static class UpdateFollowed
{
    private static void Main()
    {
        const string thisProgram = "Update Followed";
        Console.WriteLine($"{DateTime.Now}: {thisProgram}");
        AppInfo appInfo = new("TVMaze", thisProgram, "DbAlternate");
        var     log     = appInfo.TxtFile;
        log.Start();

        LogModel.Record(thisProgram, "Main", "Starting Processing", 3);

        using WebApi tvmApi = new(appInfo);
        var          gfs    = tvmApi.GetFollowedShows();

        if (tvmApi.IsTimedOut)
        {
            log.Write("Getting an Time Out twice on the GetFollowedShows call to TVMaze");
            LogModel.Record(thisProgram, "Main", "Getting an TimeOut twice on the Get FollowedShows call to TVMaze", 3);
            LogModel.Record(thisProgram, "Main", "Stopping Processing",                                              3);
            Environment.Exit(99);
        }

        var followedShowOnTvmaze = tvmApi.ConvertHttpToJArray(gfs);
        log.Write($"Found {followedShowOnTvmaze.Count} Followed Shows Tvmaze", "", 2);
        LogModel.Record(thisProgram, "Main", $"Found {followedShowOnTvmaze.Count} Followed Shows Tvmaze", 3);

        CheckDb cdb     = new();
        var     records = CheckDb.FollowedCount(appInfo);
        log.Write($"There are {records} records in Following Table", "", 2);
        LogModel.Record(thisProgram, "Main", $"There are {records} records in Following Table", 3);

        Show      theShow          = new(appInfo);
        var       idx              = 0;
        var       delIdx           = 0;
        List<int> allFollowedShows = new();

        using MariaDb mDbWrite        = new(appInfo);
        Followed      inFollowedTable = new(appInfo);

        foreach (var show in followedShowOnTvmaze)
        {
            var jtShow = int.Parse(show["show_id"]!.ToString());
            allFollowedShows.Add(jtShow);

            log.Write($"Processing {jtShow}", "", 4);
            LogModel.Record(thisProgram, "Main", $"Processing {jtShow}", 5);
            inFollowedTable.GetFollowed(jtShow);

            if (inFollowedTable.InDb)
            {
                using (UpdateTvmStatus uts = new())
                {
                    uts.ToFollowed(appInfo, jtShow);
                }

                idx++;
            } else
            {
                theShow.FillViaTvmaze(jtShow);

                if (theShow.Finder != "Skip" && theShow.UpdateDate != "2200-01-01")
                    theShow.TvmStatus                                                                      = "Following";
                else if (theShow.Finder == "Skip" || theShow.UpdateDate == "2200-01-01") theShow.TvmStatus = "Skipping";

                using MariaDb tsu  = new(appInfo);
                var           rows = tsu.ExecNonQuery($"update TvmShowUpdates set `TvmUpdateEpoch` = {theShow.TvmUpdatedEpoch} where `TvmShowId` = {theShow.TvmShowId};");

                if (rows == 0)
                {
                    rows = tsu.ExecNonQuery($"insert into TvmShowUpdates values (0, {theShow.TvmShowId}, {theShow.TvmUpdatedEpoch}, 0);");

                    if (rows == 1)
                    {
                        log.Write($"Updated the TvmShowUpdates table with {theShow.TvmUpdatedEpoch}");
                        LogModel.Record(thisProgram, "Main", $"Updated the TvmShowUpdates table with {theShow.TvmUpdatedEpoch}", 3);
                    } else
                    {
                        log.Write($"Failed to Insert the TvmShowUpdates table with {theShow.TvmUpdatedEpoch}");
                        LogModel.Record(thisProgram, "Main", $"Failed to Insert the TvmShowUpdates table with {theShow.TvmUpdatedEpoch}", 3);

                        continue;
                    }
                }

                if (theShow.IsDbFilled)
                    theShow.DbUpdate();
                else
                    theShow.DbInsert(true);

                theShow.Reset();
                inFollowedTable.DbInsert(true);

                using (ShowAndEpisodes sae = new(appInfo))
                {
                    log.Write($"Working on Refreshing Show {jtShow}");
                    sae.Refresh(jtShow);
                }

                idx++;

                inFollowedTable.Reset();

                //allFollowedShows.Add(int.Parse(show["show_id"].ToString()));
                mDbWrite.Close();
            }
        }

        log.Write($"Updated or Inserted {idx} Shows", "", 2);
        LogModel.Record(thisProgram, "Main", $"Updated or Inserted {idx} Shows", 3);

        // Delete Shows that are no longer being followed with a limit of 10 at a time.
        Followed followed = new(appInfo);
        var      toDelete = followed.ShowsToDelete(allFollowedShows);

        if (toDelete.Count > 0)
        {
            if (toDelete.Count <= 10)
                foreach (var showId in toDelete)
                {
                    log.Write($"Deleting {showId}", "", 2);
                    theShow.DbDelete(showId);
                    theShow.Reset();
                    followed.DbDelete(showId);
                    followed.Reset();
                    delIdx++;
                }
            else
            {
                log.Write($"Too Many Shows are flagged for deletion {toDelete.Count}", "", 0);
                LogModel.Record(thisProgram, "Main", $"Too Many Shows are flagged for deletion {toDelete.Count}", 3);
            }

            log.Write($"Deleted {delIdx} Shows", "", 1);
            LogModel.Record(thisProgram, "Main", $"Deleted {delIdx} Shows", 3);
        }

        MariaDb mdb  = new(appInfo);
        MariaDb mDbW = new(appInfo);
        var     rdr  = mdb.ExecQuery("select ShowsTvmShowId from NotInFollowed where `Status` = 'Following'");

        while (rdr.Read())
        {
            mDbW.ExecQuery($"update Shows set `TvmStatus` = 'New' where `TvmShowId` = {int.Parse(rdr[0].ToString()!)}");
            log.Write($"Reset {rdr[0]} to New ---> Should not occur ###################", "", 0);
            LogModel.Record(thisProgram, "Main", $"Reset {rdr[0]} to New ---> Should not occur ###################", 6);
        }

        log.Stop();
        LogModel.Record(thisProgram, "Main", "Stopping Processing", 3);
    }
}
