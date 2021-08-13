using Common_Lib;
using DB_Lib;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net.Http;
using Web_Lib;

namespace DataBase
{
    class Program
    {
        static void Main()
        {
            Stopwatch watch = new();
            watch.Start();

            string[] newpath = new string[] { "Users", "Dick", "TVMaze", "Logs" };
            AppInfo app1info = new("Database", "ProductionDB", "CheckTvmShowUpdates.log", newpath);
            AppInfo appinfo = new("Database", "Tvm-Test-DB", "CheckTvmShowUpdates.log", newpath);
            AppInfo app2info = new("Database", "ProdDB", "CheckTvmShowUpdates.log", newpath);
            TextFileHandler log = app1info.TxtFile;
            log.Start();

            #region DB Example
            
            log.Write("Connection to the MariaDB Test-TVM-DB with wrong DB Connection");
            using (MariaDB MDb = new(app1info))
            {
                if (!MDb.success)
                {
                    log.Write($"Exception is: {MDb.exception.Message}", "DB Exception", 0);
                }
            }
            log.Empty(2);

            MySqlConnector.MySqlDataReader records;
            using (MariaDB MDb = new(app2info))
            {
                log.Write("Opening the connection to the MariaDB Test-TVM-DB");
                if (!MDb.success)
                {
                    log.Write($"Open Exception is: {MDb.exception.Message}");
                    log.Empty();
                }
                log.Write("Reading key_values");
                MDb.Command("Select * from key_values");
                if (!MDb.success)
                {
                    log.Write($"Command Exception is: {MDb.exception.Message}");
                }
                records = MDb.ExecQuery();
                if (!MDb.success)
                {
                    log.Write($"ExecQuery Exception is: {MDb.exception.Message}");
                }
                else
                {
                    log.Write($"ExecQuery result is: {records.Depth} and {records.FieldCount}");
                }
            }

            using (MariaDB MDb = new(appinfo))
            {
                log.Write("Executing a query via the overloaded ExecQuery method passing in the query directly", "Program", 3);
                records = MDb.ExecQuery("Select * from download_options");
                if (!MDb.success)
                {
                    log.Write($"ExecQuery Exception is: {MDb.exception.Message}", "Read Output", 0);
                }
                else
                {
                    log.Write($"ExecQuery result is: {records.Depth} and {records.FieldCount}", "Read Output", 3);
                }
                while (records.Read())
                {
                    log.Write($"Prov Name: {records["providername"],-30}", "Read Output", 3);
                }
            }
            
            #endregion

            #region TVMaze API
           
            WebAPI tvmapi = new(log);
            log.Write("Start to API test", "Program", 0);
            HttpResponseMessage result = tvmapi.GetShow("Eden: Untamed Planet");
            log.Write($"Result back from API call {result.StatusCode}", "Program WebAPI", 3);

            var content = result.Content.ReadAsStringAsync().Result;
            dynamic jsoncontent = JsonConvert.DeserializeObject(content);

            log.Write($"JSon is {jsoncontent}");
          
            tvmapi.Dispose();
            
            #endregion

            #region Testing Rarbg
            
            tvmapi = new(log);
            log.Write("Start to Rarbg API test", "Program", 0);
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
            foreach (var show in jsoncontent["torrent_results"])
            {
                magnet = show["download"];
                log.Write($"magnet found: {magnet}");
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
