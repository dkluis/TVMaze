using System;
using System.Threading;
using Common_Lib;
using DB_Lib;
using Entities_Lib;
using MySqlConnector;

namespace RefreshShows
{
    internal class RefreshShows
    {
        private static void Main()
        {
            var This_Program = "Refresh Shows";
            Console.WriteLine($"{DateTime.Now}: {This_Program}");
            AppInfo appinfo = new("TVMaze", This_Program, "DbAlternate");
            var log = appinfo.TxtFile;
            log.Start();

            MariaDB Mdbr = new(appinfo);
            MySqlDataReader rdr;

            rdr = Mdbr.ExecQuery("select `TvmShowId` from showstorefresh limit 175");

            while (rdr.Read())
                using (ShowAndEpisodes sae = new(appinfo))
                {
                    log.Write($"Working on Show not updated in 7 days {rdr[0]}", "", 2);
                    sae.Refresh(int.Parse(rdr[0].ToString()));
                    Thread.Sleep(1000);
                }

            Mdbr.Close();


            rdr = Mdbr.ExecQuery("select distinct `TvmShowId` from episodesfromtodayback order by `TvmShowId` desc;");
            while (rdr.Read())
                using (ShowAndEpisodes sae = new(appinfo))
                {
                    log.Write($"Working on Today's Show {rdr[0]}", "", 2);
                    sae.Refresh(int.Parse(rdr[0].ToString()));
                    Thread.Sleep(1000);
                }

            log.Stop();
        }
    }
}