using Common_Lib;
using DB_Lib;
using Entities_Lib;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Web_Lib;


namespace UpdateFollowed
{
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
            log.Write($"Found {FollowedShowOnTvmaze.Count} Followed Shows Tvmaze");

            CheckDb cdb = new();
            int records = cdb.FollowedCount(appinfo);
            log.Write($"There are {records} records in Following Table", "", 2);

            Show theshow = new(appinfo);
            int idx = 0;
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
                    }
                    else
                    {
                        InFollowedTable.DbInsert();
                        theshow.FillViaTvmaze(jtshow);
                        theshow.TvmStatus = "Following";
                        if (theshow.isDBFilled) { theshow.DbUpdate(); } else { theshow.DbInsert(); }
                        theshow.Reset();
                    }
                    InFollowedTable.Reset();

                    AllFollowedShows.Add(int.Parse(show["show_id"].ToString()));
                    idx++;
                    Mdbw.Close();
                }
                log.Write($"Updated or Inserted {idx} Followed Shows");
            }

            Followed followed = new(appinfo);
            List<int> ToDelete = followed.ShowsToDelete(AllFollowedShows);

            if (ToDelete.Count > 0)
            {
                foreach (int showid in ToDelete)
                {
                    log.Write($"Need to Delete {showid}", "", 2);
                    theshow.DbDelete(showid);
                    theshow.Reset();
                    followed.DbDelete(showid);
                    followed.Reset();

                }
            }

            if (Math.Abs(FollowedShowOnTvmaze.Count - records) > 10)
            {
                log.Write($"Skipping this program since too many records are going to be deleted: {Math.Abs(FollowedShowOnTvmaze.Count - records)}");
                using (ActionItems ai = new(appinfo)) { ai.DbInsert($"Skipping this program since too many records are going to be deleted: {Math.Abs(FollowedShowOnTvmaze.Count - records)}"); }
                log.Write($"###################################################################################################################");
                log.Stop();
                Environment.Exit(999);
            }

            log.Stop();
            Console.WriteLine($"{DateTime.Now}: {This_Program} Finished");
        }
    }
}