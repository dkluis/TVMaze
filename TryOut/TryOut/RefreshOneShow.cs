using Common_Lib;
using DB_Lib;
using Entities_Lib;

namespace RefreshOneShow
{
    internal static class RefreshOneShow
    {
        private static void Main()
        {
            const string thisProgram = "Refresh One Show";
            AppInfo appInfo = new("TVMaze", thisProgram, "DbAlternate");
            var log = appInfo.TxtFile;
            log.Start();

            const int theShowToRefresh = 49333;

            MariaDb mDbR = new(appInfo);

            var rdr = mDbR.ExecQuery(
                $"select `TvmShowId`, `ShowName` from Shows where `TvmShowId` = {theShowToRefresh} order by `TvmShowId` desc");

            while (rdr.Read())
            {
                using ShowAndEpisodes sae = new(appInfo);
                log.Write($"Working on Show {rdr[0]} {rdr[1]}", "", 2);
                sae.Refresh(int.Parse(rdr[0].ToString()!));
            }

            log.Stop();
        }
    }
}