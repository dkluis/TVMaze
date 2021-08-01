using DB_Lib;
using System;
using Common_Lib;
using System.Diagnostics;
using Web_Lib;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace DataBase
{
    class Program
    {
        static void Main()
        {
            Stopwatch watch = new();
            watch.Start();
            Console.WriteLine("TVMaze Test Console Started");

            Logger log = new();
            log.Start("TVMaze Console App");

            #region DB Example
            /*
            log.Write("Connection to the MariaDB Test-TVM-DB with wrong password", "Program", 3);
            using (MariaDB MDb = new("server=ca-server.local; database=Test-TVM-DB; uid=dick; pwd=WrongPassword", log))
            {
                if (!MDb.success)
                {
                    log.Write($"Exception is: {MDb.exception.Message}", "Wrong Password", 0);
                }
            }

            using (MariaDB MDb = new("", log))
            {
                log.Write("Opening the connection to the MariaDB Test-TVM-DB with correct password", "Program", 3);
                if (!MDb.success)
                {
                    log.Write($"Open Exception is: {MDb.exception.Message}", "Correct Password", 0);
                }
                MDb.Command("Select * from key_values");
                if (!MDb.success)
                {
                    log.Write($"Command Exception is: {MDb.exception.Message}", "Correct Password", 0);
                }
                MySqlConnector.MySqlDataReader records = MDb.ExecQuery();
                if (!MDb.success)
                {
                    log.Write($"ExecQuery Exception is: {MDb.exception.Message}", "Correct Password", 0);
                }
                else
                {
                    log.Write($"ExecQuery result is: {records.Depth} and {records.FieldCount}", "Correct Password", 3);
                }
            }

            using (MariaDB MDb = new("ProdDB", log))
            {
                log.Write("Executing a query via the overloaded ExecQuery method passing in the query directly", "Program", 3);
                MySqlConnector.MySqlDataReader records = MDb.ExecQuery("Select * from download_options");
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
                    log.Write($"Prov Name: {records["providername"].ToString().PadRight(30)}", "Read Output", 3);
                }
            }
            */
            #endregion

            #region WebAPI Example
            /*
            WebAPI tvmapi = new(log);
            log.Write("Start to API test", "Program", 0);
            HttpResponseMessage result = tvmapi.GetShow("DC's Legends of Tomorrow");
            log.Write($"Result back from API call {result.StatusCode}", "Program WebAPI", 3);
            log.Write($"Result headers are {result.Content.Headers}", "Program WebAPI", 3);

            var content = result.Content.ReadAsStringAsync().Result;
            dynamic jsoncontent = JsonConvert.DeserializeObject(content);
            //log.Write($"Content is {content}, Type is {content.GetType()}");
            log.Write($"JSon is {jsoncontent}");

            WebAPI.Dispose();
            */
            #endregion

            #region WebScrap Example
            
            WebScrape scrape = new(log);
            /*
            List<string> magnets = scrape.TestWebScrap();
            log.Write($"Number of Magnets found: {magnets.Count}");
            */
            scrape.GetMagnetTVShowEpisode("Show", "S01E01");
            scrape.Dispose();
            #endregion

            log.Write($"Program executed in {watch.ElapsedMilliseconds} mSec", "Program", 1);
            //Console.Read();
            log.Stop();
            Console.WriteLine("Done");
        }
    }
}
