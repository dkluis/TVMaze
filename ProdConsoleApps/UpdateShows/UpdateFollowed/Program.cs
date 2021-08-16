using System;

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
            Console.WriteLine($"Progress can be followin in {appinfo.FullPath}");
            TextFileHandler log = appinfo.TxtFile;
            log.Start();

            WebAPI tvmapi = new(appinfo);
            JArray jsoncontent = tvmapi.ConvertHttpToJArray(tvmapi.GetFollowedShows());
            log.Write($"Found {jsoncontent.Count} Followed Shows Tvmaze", This_Program, 0);

            //#TODO Get the # of shows in the Followed Table.

            int idx = 0;
            using (MariaDB Mdbw = new(appinfo))
            {
                Followed followed = new(appinfo);

                foreach (JToken show in jsoncontent)
                {
                    log.Write($"Show is {show["show_id"]}");
                    followed.Fill(Int32.Parse(show["show_id"].ToString()), "");
                    if (!followed.DbUpdate(true)) { followed.DbInsert(); };
                    followed.Reset();
                    idx++;
                }
                log.Write($"Updated or Inserted {idx} Followed Shows");
                Mdbw.Close();
            }

            //TODO Compare Old # for Followed shows to new # and Cascade Delete everything for the deleted shows.

            log.Stop();
            Console.WriteLine($"Finished {This_Program} Program");
        }
    }
}