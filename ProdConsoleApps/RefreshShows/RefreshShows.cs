using Common_Lib;
using DB_Lib;
using Entities_Lib;

namespace RefreshShows;

internal static class RefreshShows
{
    private static void Main()
    {
        const string thisProgram = "Refresh Shows";
        Console.WriteLine($"{DateTime.Now}: {thisProgram}");
        AppInfo appInfo = new("TVMaze", thisProgram, "DbAlternate");
        var log = appInfo.TxtFile;
        log.Start();

        MariaDb mDbR = new(appInfo);

        // Get all Shows to refresh today 
        var rdr = mDbR.ExecQuery("select `TvmShowId` from showstorefresh where `TvmStatus` != 'Skipping' limit 300");
        while (rdr.Read())
        {
            using ShowAndEpisodes sae = new(appInfo);
            log.Write($"Working on Show not updated in 7 to 31 days {rdr[0]}", "", 2);
            sae.Refresh(int.Parse(rdr[0].ToString()!));
            //Thread.Sleep(1000);
        }

        mDbR.Close();

        // Get all shows to refresh that have episodes that without a broadcast date
        rdr = mDbR.ExecQuery(
            "select distinct `TvmShowId` from episodesfullinfo where `TvmStatus` != 'Skipping' and `UpdateDate` != `ShowUpdateDate` and `PlexDate` is not NULL order by `TvmShowId` desc;");
        while (rdr.Read())
        {
            using ShowAndEpisodes sae = new(appInfo);
            log.Write($"Working on Epi Update for Episodes without a Broadcast Date {rdr[0]}", "", 2);
            sae.Refresh(int.Parse(rdr[0].ToString()!));
            //Thread.Sleep(1000);
        }

        mDbR.Close();

        // Refresh all shows with Orphaned Episodes 
        rdr = mDbR.ExecQuery("select distinct `TvmShowId` from orphanedepisodes order by `TvmShowId`;");
        while (rdr.Read())
        {
            using ShowAndEpisodes sae = new(appInfo);
            log.Write($"Working on Epi Update for Orphaned Episodes {rdr[0]}", "", 2);
            sae.Refresh(int.Parse(rdr[0].ToString()!));
            //Thread.Sleep(1000);
        }

        mDbR.Close();

        // Get all Shows that will need to be acquired today to refresh
        rdr = mDbR.ExecQuery("select distinct `TvmShowId` from episodesfromtodayback order by `TvmShowId` desc;");
        while (rdr.Read())
        {
            using ShowAndEpisodes sae = new(appInfo);
            log.Write($"Working on Today's Show {rdr[0]}", "", 2);
            sae.Refresh(int.Parse(rdr[0].ToString()!));
            //Thread.Sleep(1000);
        }

        mDbR.Close();
        
        log.Stop();
    }
}