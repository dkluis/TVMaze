using Common_Lib;
using DB_Lib;
using Entities_Lib;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Web_Lib;


namespace UpdateFollowed
{
    
    /// <summary>
    ///
    ///     1. Gets all Followed marked shows from Tvmaze Web
    ///     2. All Tvmaze Web show are evaluated and updated or inserted based on what is in Tvmaze Local and are marked as Following
    ///     3. Deletes all Shows that were followed before but have been unfollowed
    ///     
    /// </summary>
    class UpdateFollowed
    {
        private static void Main()
        {
            string This_Program = "Update Followed";
            Console.WriteLine($"{DateTime.Now}: {This_Program} Started");
            AppInfo appinfo = new("TVMaze", This_Program, "DbAlternate");
            Console.WriteLine($"{DateTime.Now}: {This_Program} Progress can be followed in {appinfo.FullPath}");
            TextFileHandler log = appinfo.TxtFile;
            log.Start();

            WebAPI tvmapi = new(appinfo);
            JArray FollowedShowOnTvmaze = tvmapi.ConvertHttpToJArray(tvmapi.GetFollowedShows());
            log.Write($"Found {FollowedShowOnTvmaze.Count} Followed Shows Tvmaze", "", 2);

            CheckDb cdb = new();
            int records = cdb.FollowedCount(appinfo);
            log.Write($"There are {records} records in Following Table", "", 2);

            Show theshow = new(appinfo);
            int idx = 0;
            int delidx = 0;
            List<int> AllFollowedShows = new();

            using (MariaDB Mdbw = new(appinfo))
            {
                Followed InFollowedTable = new(appinfo);
                int jtshow;

                foreach (JToken show in FollowedShowOnTvmaze)
                {
                    jtshow = int.Parse(show["show_id"].ToString());

                    log.Write($"Processing {jtshow}", "", 4);
                    InFollowedTable.GetFollowed(jtshow);

                    if (InFollowedTable.inDB)
                    {
                        using (UpdateTvmStatus uts = new()) { uts.ToFollowed(appinfo, jtshow); }
                        idx++;
                    }
                    else
                    {
                        theshow.FillViaTvmaze(jtshow);
                        theshow.TvmStatus = "Following";
                        if (theshow.isDBFilled) { theshow.DbUpdate(); } else { theshow.DbInsert(); }
                        using (MariaDB tsu = new(appinfo))
                        {
                            tsu.ExecNonQuery($"update TvmShowUpdates set `TvmUpdateEpoch` = {theshow.TvmUpdatedEpoch} where `TvmShowId` = {theshow.TvmShowId};");
                            log.Write($"Updated the TvmShowUpdates table with {theshow.TvmUpdatedEpoch}", "", 3);
                        }
                        theshow.Reset();
                        InFollowedTable.DbInsert();
                        using (ShowAndEpisodes sae = new(appinfo))
                        {
                            log.Write($"Working on Refreshing Show {jtshow}", "", 3);
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
            List<int> ToDelete = followed.ShowsToDelete(AllFollowedShows);
            if (ToDelete.Count > 0)
            {
                if (ToDelete.Count <= 10)
                {
                    foreach (int showid in ToDelete)
                    {
                        log.Write($"Deleting {showid}", "", 2);
                        theshow.DbDelete(showid);
                        theshow.Reset();
                        followed.DbDelete(showid);
                        followed.Reset();
                        delidx++;
                    }
                }
                else { log.Write($"Too Many Shows are flagged for deletion {ToDelete.Count}", "", 0); }
                log.Write($"Deleted {delidx} Shows", "", 1);
            }


            log.Stop();
            Console.WriteLine($"{DateTime.Now}: {This_Program} Finished");
        }
    }
}