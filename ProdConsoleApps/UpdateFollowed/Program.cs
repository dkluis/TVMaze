using System;
using System.Collections.Generic;

using Common_Lib;
using Web_Lib;
using DB_Lib;
using TvmEntities;

using Newtonsoft.Json.Linq;


namespace This_Program
{
    class Program
    {
        private static void Main()
        {
            string This_Program = "Update Followed";
            Console.WriteLine($"Starting {This_Program}");
            AppInfo appinfo = new("TVMaze", This_Program, "DbAlternate");
            AppInfo shows = new("TVMaze", This_Program, "DbAlternate");
            Console.WriteLine($"Progress can be followin in {appinfo.FullPath}");
            TextFileHandler log = appinfo.TxtFile;
            log.Start();

            WebAPI tvmapi = new(appinfo);
            JArray jsoncontent = tvmapi.ConvertHttpToJArray(tvmapi.GetFollowedShows());
            log.Write($"Found {jsoncontent.Count} Followed Shows Tvmaze", This_Program, 0);

            //#TODO Get the # of shows in the Followed Table.

            int idx = 0;
            List<int> AllFollowedShows = new();
            using (MariaDB Mdbw = new(appinfo))
            {
                Followed following = new(appinfo);

                foreach (JToken show in jsoncontent)
                {
                    // log.Write($"Show is {show["show_id"]}");
                    following.Fill(Int32.Parse(show["show_id"].ToString()), "");
                    AllFollowedShows.Add(Int32.Parse(show["show_id"].ToString()));
                    // Test Condition below to be taken out for production
                    // if (Int32.Parse(show["show_id"].ToString()) > 10) { AllFollowedShows.Add(Int32.Parse(show["show_id"].ToString())); }
                    if (!following.DbUpdate(true)) { following.DbInsert(); };
                    following.Reset();
                    idx++;
                }
                log.Write($"Updated or Inserted {idx} Followed Shows");
                Mdbw.Close();
            }

            Followed deletefollowed = new(appinfo);


            List<int> deletethese = deletefollowed.ShowsToDelete(AllFollowedShows);
            log.Write($"Count of shows To Delete {deletethese.Count}");

            Followed followed = new(appinfo);

            using (Show dshow = new(appinfo))
            {
                foreach (int showid in deletethese)
                {
                    dshow.FillViaTvmaze(showid);
                    if (dshow.DbDelete())
                    {
                        log.Write($"ShowId Deleted from Shows: {showid}");
                        //delete the show itself from Followed
                        followed.Fill(showid, "");
                        if (followed.DbDelete())
                        { log.Write($"Show is also deleted"); } else { log.Write($"Show deletion error"); }
                        followed.Reset();
                    }
                    else { log.Write($"Delete Failed for ShowId {showid}"); }
                    
                }
                dshow.Reset();
                dshow.CloseDB();
            }

            log.Stop();
            Console.WriteLine($"Finished {This_Program} Program");
        }
    }
}