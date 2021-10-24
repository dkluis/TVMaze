using System;
using Common_Lib;
using DB_Lib;
using Web_Lib;

namespace AcquireMovies
{
    class Program
    {
        static void Main(string[] args)
        {
            string This_Program = "Acquire Movies";
            Console.WriteLine($"{DateTime.Now}: {This_Program}");
            AppInfo appinfo = new("TVMaze", This_Program, "DbAlternate");
            TextFileHandler log = appinfo.TxtFile;
            log.Start();

            MariaDB mdb = new(appinfo);
            string sql = $"select * from Movies";  //Todo add where clause for date comparision later
            MySqlConnector.MySqlDataReader rdr = mdb.ExecQuery(sql);
            string name = "";
            string seriesName = "";
            int movieNumber = 0;
            string finderDate = "";
            string mediaType = "";

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