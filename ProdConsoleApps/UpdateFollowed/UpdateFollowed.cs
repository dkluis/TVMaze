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
            var This_Program = "Update Followed";
            Console.WriteLine($"{DateTime.Now}: {This_Program}");
            AppInfo appinfo = new("TVMaze", This_Program, "DbAlternate");
            var log = appinfo.TxtFile;
            log.Start();

            WebAPI tvmapi = new(appinfo);
            var gfs = tvmapi.GetFollowedShows();
            if (tvmapi.isTimedOut)
            {
                log.Write("Getting an Time Out twice on the GetFollowedShows call to TVMaze");
                Environment.Exit(99);
            }

            var FollowedShowOnTvmaze = tvmapi.ConvertHttpToJArray(gfs);
            log.Write($"Found {FollowedShowOnTvmaze.Count} Followed Shows Tvmaze", "", 2);

            CheckDb cdb = new();
            var records = cdb.FollowedCount(appinfo);
            log.Write($"There are {records} records in Following Table", "", 2);

            Show theshow = new(appinfo);
            var idx = 0;
            var delidx = 0;
            List<int> AllFollowedShows = new();

            using (MariaDB Mdbw = new(appinfo))
            {
                Followed InFollowedTable = new(appinfo);
                int jtshow;

                foreach (var show in FollowedShowOnTvmaze)
                {
                    jtshow = int.Parse(show["show_id"].ToString());

                    log.Write($"Processing {jtshow}", "", 4);
                    InFollowedTable.GetFollowed(jtshow);

                    if (InFollowedTable.inDB)
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
                        using (MariaDB tsu = new(appinfo))
                        {
                            tsu.ExecNonQuery(
                                $"update TvmShowUpdates set `TvmUpdateEpoch` = {theshow.TvmUpdatedEpoch} where `TvmShowId` = {theshow.TvmShowId};");
                            log.Write($"Updated the TvmShowUpdates table with {theshow.TvmUpdatedEpoch}");
                        }

                        theshow.Reset();
                        InFollowedTable.DbInsert(true);
                        using (ShowAndEpisodes sae = new(appinfo))
                        {
                            log.Write($"Working on Refreshing Show {jtshow}");
                            sae.Refresh(jtshow);
                        }

                        idx++;
                    }

                    InFollowedTable.Reset();
                    AllFollowedShows.Add(int.Parse(show["show_id"].ToString()));
                    Mdbw.Close();
                }

                log.Write($"Updated or Inserted {idx} Shows", "", 2);
            }

            Followed followed = new(appinfo);
            var ToDelete = followed.ShowsToDelete(AllFollowedShows);
            if (ToDelete.Count > 0)
            {
                if (ToDelete.Count <= 10)
                    foreach (var showid in ToDelete)
                    {
                        log.Write($"Deleting {showid}", "", 2);
                        theshow.DbDelete(showid);
                        theshow.Reset();
                        followed.DbDelete(showid);
                        followed.Reset();
                        delidx++;
                    }
                else
                    log.Write($"Too Many Shows are flagged for deletion {ToDelete.Count}", "", 0);

                log.Write($"Deleted {delidx} Shows", "", 1);
            }

            MariaDB mdb = new(appinfo);
            MariaDB mdbw = new(appinfo);
            var rdr = mdb.ExecQuery("select ShowsTvmShowId from notinfollowed where `Status` = 'Following'");
            while (rdr.Read())
            {
                mdbw.ExecQuery(
                    $"update Shows set `TvmStatus` = 'New' where `TvmShowid` = {int.Parse(rdr[0].ToString())}");
                log.Write($"Reset {rdr[0]} to New ---> Should not occur ###################", "", 0);
            }

            log.Stop();
        }
    }
}