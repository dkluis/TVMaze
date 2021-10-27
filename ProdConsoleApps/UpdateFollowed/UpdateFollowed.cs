using System;
using System.Collections.Generic;
using Common_Lib;
using DB_Lib;
using Entities_Lib;
using Web_Lib;

namespace UpdateFollowed
{
    /// <summary>
    ///     1. Gets all Followed marked shows from Tvmaze Web
    ///     2. All Tvmaze Web show are evaluated and updated or inserted based on what is in Tvmaze Local and are marked as
    ///     Following
    ///     3. Deletes all Shows that were followed before but have been unfollowed
    /// </summary>
    internal class UpdateFollowed
    {
        private static void Main()
        {
            var thisProgram = "Update Followed";
            Console.WriteLine($"{DateTime.Now}: {thisProgram}");
            AppInfo appinfo = new("TVMaze", thisProgram, "DbAlternate");
            var log = appinfo.TxtFile;
            log.Start();

            WebApi tvmapi = new(appinfo);
            var gfs = tvmapi.GetFollowedShows();
            if (tvmapi.IsTimedOut)
            {
                log.Write("Getting an Time Out twice on the GetFollowedShows call to TVMaze");
                Environment.Exit(99);
            }

            var followedShowOnTvmaze = tvmapi.ConvertHttpToJArray(gfs);
            log.Write($"Found {followedShowOnTvmaze.Count} Followed Shows Tvmaze", "", 2);

            CheckDb cdb = new();
            var records = cdb.FollowedCount(appinfo);
            log.Write($"There are {records} records in Following Table", "", 2);

            Show theshow = new(appinfo);
            var idx = 0;
            var delidx = 0;
            List<int> allFollowedShows = new();

            using (MariaDb mDbWrite = new(appinfo))
            {
                Followed inFollowedTable = new(appinfo);
                int jtshow;

                foreach (var show in followedShowOnTvmaze)
                {
                    jtshow = int.Parse(show["show_id"].ToString());

                    log.Write($"Processing {jtshow}", "", 4);
                    inFollowedTable.GetFollowed(jtshow);

                    if (inFollowedTable.InDb)
                    {
                        using (UpdateTvmStatus uts = new())
                        {
                            uts.ToFollowed(appinfo, jtshow);
                        }

                        idx++;
                    }
                    else
                    {
                        theshow.FillViaTvmaze(jtshow);
                        theshow.TvmStatus = "Following";
                        if (theshow.IsDbFilled)
                            theshow.DbUpdate();
                        else
                            theshow.DbInsert(true);
                        using (MariaDb tsu = new(appinfo))
                        {
                            tsu.ExecNonQuery(
                                $"update TvmShowUpdates set `TvmUpdateEpoch` = {theshow.TvmUpdatedEpoch} where `TvmShowId` = {theshow.TvmShowId};");
                            log.Write($"Updated the TvmShowUpdates table with {theshow.TvmUpdatedEpoch}");
                        }

                        theshow.Reset();
                        inFollowedTable.DbInsert(true);
                        using (ShowAndEpisodes sae = new(appinfo))
                        {
                            log.Write($"Working on Refreshing Show {jtshow}");
                            sae.Refresh(jtshow);
                        }

                        idx++;
                    }

                    inFollowedTable.Reset();
                    allFollowedShows.Add(int.Parse(show["show_id"].ToString()));
                    mDbWrite.Close();
                }

                log.Write($"Updated or Inserted {idx} Shows", "", 2);
            }

            Followed followed = new(appinfo);
            var toDelete = followed.ShowsToDelete(allFollowedShows);
            if (toDelete.Count > 0)
            {
                if (toDelete.Count <= 10)
                    foreach (var showid in toDelete)
                    {
                        log.Write($"Deleting {showid}", "", 2);
                        theshow.DbDelete(showid);
                        theshow.Reset();
                        followed.DbDelete(showid);
                        followed.Reset();
                        delidx++;
                    }
                else
                    log.Write($"Too Many Shows are flagged for deletion {toDelete.Count}", "", 0);

                log.Write($"Deleted {delidx} Shows", "", 1);
            }

            MariaDb mdb = new(appinfo);
            MariaDb mDbW = new(appinfo);
            var rdr = mdb.ExecQuery("select ShowsTvmShowId from notinfollowed where `Status` = 'Following'");
            while (rdr.Read())
            {
                mDbW.ExecQuery(
                    $"update Shows set `TvmStatus` = 'New' where `TvmShowid` = {int.Parse(rdr[0].ToString())}");
                log.Write($"Reset {rdr[0]} to New ---> Should not occur ###################", "", 0);
            }

            log.Stop();
        }
    }
}