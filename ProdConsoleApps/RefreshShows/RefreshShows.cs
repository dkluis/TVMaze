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
            Console.WriteLine($"{DateTime.Now}: {This_Program}");
            AppInfo appinfo = new("TVMaze", This_Program, "DbAlternate");
            TextFileHandler log = appinfo.TxtFile;
            log.Start();

            MariaDB Mdbr = new(appinfo);
            MySqlConnector.MySqlDataReader rdr;

            rdr = Mdbr.ExecQuery($"select `TvmShowId` from Shows where " +
                $"(ShowStatus != 'Ended' and (date(PremiereDate) = '1900-01-01' or date(PremiereDate) > '2014-01-01') and UpdateDate < CURDATE() - 6) " +
                $"or (ShowStatus = 'Ended' and (date(PremiereDate) = '1900-01-01' and UpdateDate < CURDATE() - 6)) " +
                $"order by `TvmShowId` desc");
            
            while (rdr.Read())
            {
                using (ShowAndEpisodes sae = new(appinfo))
                {
                    log.Write($"Working on Show not updated in 7 days {rdr[0]}", "", 2);
                    sae.Refresh(int.Parse(rdr[0].ToString()));
                    System.Threading.Thread.Sleep(1000);
                }
            }

            Mdbr.Close();


            rdr = Mdbr.ExecQuery($"select distinct `TvmShowId` from episodestoacquire order by `TvmShowId` desc;");
            while (rdr.Read())
            {
                using (ShowAndEpisodes sae = new(appinfo))
                {
                    log.Write($"Working on Today's Show {rdr[0]}", "", 2);
                    sae.Refresh(int.Parse(rdr[0].ToString()));
                    System.Threading.Thread.Sleep(1000);
                }
            }

            log.Stop();
        }
    }
}