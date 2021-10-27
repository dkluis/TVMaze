using System;
using Common_Lib;
using DB_Lib;

namespace AcquireMovies
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var thisProgram = "Acquire Movies";
            Console.WriteLine($"{DateTime.Now}: {thisProgram}");
            AppInfo appinfo = new("TVMaze", thisProgram, "DbAlternate");
            var log = appinfo.TxtFile;
            log.Start();

            MariaDb mdb = new(appinfo);
            var sql = "select * from Movies"; //Todo add where clause for date comparision later
            var rdr = mdb.ExecQuery(sql);
            var name = "";
            var seriesName = "";
            var movieNumber = 0;
            var finderDate = "";
            var mediaType = "";

            while (rdr.Read())
            {
                name = rdr["name"].ToString();
                seriesName = rdr["SeriesName"].ToString();
                finderDate = rdr["FinderDate"].ToString();
                mediaType = rdr["MediaType"].ToString();
                movieNumber = int.Parse(rdr["MovieNumber"].ToString());
                log.Write($"Processing {name} ---> {seriesName}, {movieNumber}, {finderDate}, {mediaType}");
            }

            log.Stop();
        }
    }
}