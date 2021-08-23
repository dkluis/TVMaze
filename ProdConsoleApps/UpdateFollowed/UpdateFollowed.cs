using System;
using System.Collections.Generic;

using Common_Lib;
using Web_Lib;
using DB_Lib;
using TvmEntities;

using Newtonsoft.Json.Linq;


namespace UpdateFollowed
{
    class UpdateFollowed
    {
        private static void Main()
        {
            string This_Program = "Update Followed";
            Console.WriteLine($"{DateTime.Now}: Starting {This_Program}");
            AppInfo appinfo = new("TVMaze", This_Program, "DbAlternate");
            Console.WriteLine($"{DateTime.Now}: Progress can be followed in {appinfo.FullPath}");
            TextFileHandler log = appinfo.TxtFile;
            log.Start();

            WebAPI tvmapi = new(appinfo);
            JArray followedontvmaze = tvmapi.ConvertHttpToJArray(tvmapi.GetFollowedShows());
            log.Write($"Found {followedontvmaze.Count} Followed Shows Tvmaze");

            CheckDb cdb = new();
            int records = cdb.FollowedCount(appinfo);
            log.Write($"There are {records} records in Following Table", "", 2);

            if (Math.Abs(followedontvmaze.Count - records) > 30)
            {
                log.Write($"Skipping this program since too many records are going to be deleted: {Math.Abs(followedontvmaze.Count - records)}");
                Environment.Exit(999);
            }
            log.EmptyLine(2);


            Show theshow = new(appinfo);

            int idx = 0;
            List<int> AllFollowedShows = new();

            
            using (MariaDB Mdbw = new(appinfo))
            {
                Followed inDBasFollowing = new(appinfo);
                int jtshow;

                foreach (JToken show in followedontvmaze)
                { 
                    jtshow = int.Parse(show["show_id"].ToString());

                    log.Write($"Processing {jtshow}", "", 4);
                    inDBasFollowing.GetFollowed(jtshow);
                    if (inDBasFollowing.inDB)
                    {
                        inDBasFollowing.DbUpdate();
                        theshow.FillViaTvmaze(jtshow);
                        if (theshow.isDBFilled) { theshow.DbUpdate(); } else { theshow.DbInsert(); }
                        theshow.Reset();
                    }
                    else
                    {
                        inDBasFollowing.DbInsert();
                        if (theshow.isDBFilled) { theshow.DbUpdate(); } else { theshow.DbInsert(); }
                        theshow.Reset();
                    }

                    inDBasFollowing.Reset();

                    AllFollowedShows.Add(int.Parse(show["show_id"].ToString()));
                    idx++;
                    Mdbw.Close();
                }
                log.Write($"Updated or Inserted {idx} Followed Shows");
            }

            log.Stop();
            Console.WriteLine($"{DateTime.Now}: Finished {This_Program} Program");
        }
    }
}