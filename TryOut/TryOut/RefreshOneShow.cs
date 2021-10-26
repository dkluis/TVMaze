using Common_Lib;
using DB_Lib;
using Entities_Lib;
using MySqlConnector;

namespace RefreshOneShow
{
    internal class RefreshOneShow
    {
        private static void Main()
        {
            var This_Program = "Refresh One Show";
            AppInfo appinfo = new("TVMaze", This_Program, "DbAlternate");
            var log = appinfo.TxtFile;
            log.Start();

            var TheShowToRefresh = 58473;

            MariaDB Mdbr = new(appinfo);
            MySqlDataReader rdr;

            rdr = Mdbr.ExecQuery(
                $"select `TvmShowId`, `ShowName` from Shows where `TvmShowId` = {TheShowToRefresh} order by `TvmShowId` desc");

            while (rdr.Read())
                using (ShowAndEpisodes sae = new(appinfo))
                {
                    log.Write($"Working on Show {rdr[0]} {rdr[1]}", "", 2);
                    sae.Refresh(int.Parse(rdr[0].ToString()));
                }

            log.Stop();
        }
    }
}