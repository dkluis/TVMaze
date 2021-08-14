using Common_Lib;
using DB_Lib;
using Web_Lib;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net.Http;


namespace DataBase
{
    class Program
    {
        static void Main()
        {
            Stopwatch watch = new();
            watch.Start();

            AppInfo app1info = new("TVMaze", "ConsoleApp", "DbProduction");
            TextFileHandler log = app1info.TxtFile;
            log.Start();

            #region DB Example

            #region App1Info

            log.Write("Connection to the MariaDB Test-TVM-DB with wrong DB Connection");
            MariaDB MDb1 = new(app1info);
            MDb1.Open();
            if (!MDb1.success)
            {
                log.Write($"Exception is: {MDb1.exception.Message}", "DB Exception", 0);
            }
            MDb1.Close();
            log.EmptyLine();

            #endregion

            #region App2Info

            AppInfo app2info = new("TVMaze", "ConsoleApp", "DbProduction");
            log = app2info.TxtFile;
            MySqlConnector.MySqlDataReader records2;
            MariaDB MDb2 = new(app2info);
            log.Write("Opening the connection to the MariaDB Test-TVM-DB");
            if (!MDb2.success)
            {
                log.Write($"Open Exception is: {MDb2.exception.Message}");
                log.EmptyLine();
            }
            log.Write("Reading key_values");
            MDb2.Command("Select * from key_values");
            if (!MDb2.success)
            {
                log.Write($"Command Exception is: {MDb2.exception.Message}");
            }
            records2 = MDb2.ExecQuery();
            if (!MDb2.success)
            {
                log.Write($"ExecQuery Exception is: {MDb2.exception.Message}");
            }
            else
            {
                log.Write($"ExecQuery result is: {records2.Depth} and {records2.FieldCount}");
            }
            MDb2.Close();
            log.EmptyLine();

            #endregion

            #region AppInfo

            AppInfo appinfo = new("TVMaze", "ConsoleApp", "DbTesting"); 
            log = appinfo.TxtFile;

            MariaDB MDb = new(appinfo);
            MySqlConnector.MySqlDataReader records;
            log.Write("Executing a query via the overloaded ExecQuery method passing in the query directly");
            records = MDb.ExecQuery("Select * from download_options");
            if (!MDb.success)
            {
                log.Write($"ExecQuery Exception is: {MDb.exception.Message}", "Read Output", 0);
            }
            else
            {
                log.Write($"ExecQuery result is: {records.Depth} and {records.FieldCount}", "Read Output", 3);
                while (records.Read())
                {
                    log.Write($"Prov Name: {records["providername"],-30}", "Read Output", 3);
                }
            }
            MDb.Close();

            #endregion

            #endregion

            #region TVMaze API

            WebAPI tvmapi = new(log);
            log.Write("Start to API test");
            HttpResponseMessage result = tvmapi.GetShow("Eden: Untamed Planet");
            log.Write($"Result back from API call {result.StatusCode}", "Program WebAPI", 3);

            var content = result.Content.ReadAsStringAsync().Result;
            dynamic jsoncontent = JsonConvert.DeserializeObject(content);

            log.Write($"JSon is {jsoncontent}");

            tvmapi.Dispose();

            #endregion

            #region Testing Rarbg

            tvmapi = new(log);
            log.Write("Start to Rarbg API test");
            result = tvmapi.GetRarbgMagnets("Eden: Untamed Planet s01e02");

            log.Write($"Result back from API call {result.StatusCode}", "Program RarbgAPI", 3);

            if (!result.IsSuccessStatusCode)
            {
                Environment.Exit(99);
            }

            content = result.Content.ReadAsStringAsync().Result;
            jsoncontent = JsonConvert.DeserializeObject(content);
            // log.Write($"JSon is {jsoncontent}");

            string magnet;
            if (jsoncontent["torrent_result"] is not null)
            {
                foreach (var show in jsoncontent["torrent_results"])
                {
                    magnet = show["download"];
                    log.Write($"magnet found: {magnet}");
                }
            }

            tvmapi.Dispose();

            #endregion

            #region Getters

            MariaDB getterMdb = new(appinfo);
            getterMdb.Command("select showname, imdb from shows where `showname` = 'Hit & Run'");
            records = getterMdb.ExecQuery();
            string showname = "";
            string imdb = "";
            if (!getterMdb.success)
            {
                Environment.Exit(99);
            }

            while (records.Read())
            {
                showname = records["showname"].ToString();
                // imdb = records["imdb"].ToString();
                log.Write($"Found {showname} with {imdb} info", "Program", 3);
            }
            getterMdb.Close();

            Magnets search = new();
            magnet = search.PerformShowEpisodeMagnetsSearch(showname, 1, 2, log);

            if (magnet != "")
            {
                log.Write($"Matching magnet found: {magnet}", "Program", 3);
            }
            else
            {
                log.Write($"No matching magnet found", "Program", 3);
            }

            #endregion

            log.Write($"Program executed in {watch.ElapsedMilliseconds} mSec");
            log.Stop();

        }
    }
}
