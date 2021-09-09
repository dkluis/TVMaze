using System;

using Common_Lib;
using Entities_Lib;
using DB_Lib;

namespace RefreshShows
{
    class RefreshShows
    {
        static void Main()
        {
            string This_Program = "Refresh Shows";
            Console.WriteLine($"{DateTime.Now}: {This_Program} Started");
            AppInfo appinfo = new("TVMaze", This_Program, "DbAlternate");
            Console.WriteLine($"{DateTime.Now}: {This_Program} Progress can be followed in {appinfo.FullPath}");
            TextFileHandler log = appinfo.TxtFile;
            log.Start();

            MariaDB Mdbr = new(appinfo);
            MySqlConnector.MySqlDataReader rdr;
            rdr = Mdbr.ExecQuery($"select `TvmShowId` from Shows where `TvmStatus` = 'Following' and PremiereDate > '2014-01-01' and ShowStatus != 'Ended' order by `TvmShowId` desc;");
            //rdr = Mdbr.ExecQuery($"select `TvmShowId` from Shows where `TvmStatus` = 'Following' order by `TvmShowId` desc;");

            while (rdr.Read())
            {
                //if (int.Parse(rdr[0].ToString()) > 336) { continue; }
                using (ShowAndEpisodes sae = new(appinfo))
                {
                    log.Write($"Working on Show {rdr[0]}", "", 2);
                    sae.Refresh(int.Parse(rdr[0].ToString()));
                    System.Threading.Thread.Sleep(1000);
                }
            }

            log.Stop();
            Console.WriteLine($"{DateTime.Now}: {This_Program} Finished");
        }
    }
}