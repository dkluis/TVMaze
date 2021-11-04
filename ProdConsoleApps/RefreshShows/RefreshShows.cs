using System;
using System.Threading;
using Common_Lib;
using DB_Lib;
using Entities_Lib;

namespace RefreshShows
{
    internal static class RefreshShows
    {
        private static void Main()
        {
            var thisProgram = "Refresh Shows";
            Console.WriteLine($"{DateTime.Now}: {thisProgram}");
            AppInfo appInfo = new("TVMaze", thisProgram, "DbAlternate");
            var log = appInfo.TxtFile;
            log.Start();

            MariaDb mDbR = new(appInfo);

            var rdr = mDbR.ExecQuery("select `TvmShowId` from showstorefresh limit 175");
            while (rdr.Read())
            {
                using ShowAndEpisodes sae = new(appInfo);
                log.Write($"Working on Show not updated in 7 days {rdr[0]}", "", 2);
                sae.Refresh(int.Parse(rdr[0].ToString()!));
                Thread.Sleep(1000);
            }

            mDbR.Close();

            rdr = mDbR.ExecQuery("select distinct `TvmShowId` from episodesfromtodayback order by `TvmShowId` desc;");
            while (rdr.Read())
            {
                using ShowAndEpisodes sae = new(appInfo);
                log.Write($"Working on Today's Show {rdr[0]}", "", 2);
                sae.Refresh(int.Parse(rdr[0].ToString()!));
                Thread.Sleep(1000);
            }
            
            mDbR.Close();

            rdr = mDbR.ExecQuery("select distinct `TvmShowId` from episodesfullinfo where `UpdateDate` != `ShowUpdateDate` and `PlexDate` is not NULL order by `TvmShowId` desc;");
            while (rdr.Read())
            {
                using ShowAndEpisodes sae = new(appInfo);
                log.Write($"Working on Epi Update differences Show {rdr[0]}", "", 2);
                sae.Refresh(int.Parse(rdr[0].ToString()!));
                Thread.Sleep(1000);
            }

            log.Stop();
        }
    }
}