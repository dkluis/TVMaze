using System;
using Common_Lib;
using DB_Lib;

namespace AcquireMovies;

internal static class Program
{
    private static void Main()
    {
        const string thisProgram = "Acquire Movies";
        Console.WriteLine($"{DateTime.Now}: {thisProgram}");
        AppInfo appInfo = new("TVMaze", thisProgram, "DbAlternate");
        var     log     = appInfo.TxtFile;
        log.Start();

        MariaDb      mdb = new(appInfo);
        const string sql = "select * from Movies"; //Todo add where clause for date comparision later
        var          rdr = mdb.ExecQuery(sql);

        while (rdr.Read())
        {
            var name        = rdr["name"].ToString();
            var seriesName  = rdr["SeriesName"].ToString();
            var finderDate  = rdr["FinderDate"].ToString();
            var mediaType   = rdr["MediaType"].ToString();
            var movieNumber = int.Parse(rdr["MovieNumber"].ToString()!);
            log.Write($"Processing {name} ---> {seriesName}, {movieNumber}, {finderDate}, {mediaType}");
        }

        log.Stop();
    }
}
