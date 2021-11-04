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
            var thisProgram = "Refresh One Show";
            AppInfo appinfo = new("TVMaze", thisProgram, "DbAlternate");
            var log = appinfo.TxtFile;
            log.Start();

            var theShowToRefresh = 58473;

            MariaDb mdbr = new(appinfo);
            MySqlDataReader rdr;

            rdr = mdbr.ExecQuery(
                $"select `TvmShowId`, `ShowName` from Shows where `TvmShowId` = {theShowToRefresh} order by `TvmShowId` desc");

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