using DB_Lib;
using System;
using Common_Lib;
using System.Diagnostics;

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
            log.WriteAsync("Connection to the MariaDB Test-TVM-DB with wrong password", "Program", 3);
            using (MariaDB MDb = new("server=ca-server.local; database=Test-TVM-DB; uid=dick; pwd=WrongPassword", log))
            {
                if (!MDb.success)
                {
                    log.WriteAsync($"Exception is: {MDb.exception.Message}", "Wrong Password", 0);
                }
            }

            using (MariaDB MDb = new("", log))
            {
                log.WriteAsync("Opening the connection to the MariaDB Test-TVM-DB with correct password", "Program", 3);
                if (!MDb.success)
                {
                    log.WriteAsync($"Open Exception is: {MDb.exception.Message}", "Correct Password", 0);
                }
                MDb.Command("Select * from key_values");
                if (!MDb.success)
                {
                    log.WriteAsync($"Command Exception is: {MDb.exception.Message}", "Correct Password", 0);
                }
                MySqlConnector.MySqlDataReader records = MDb.ExecQuery();
                if (!MDb.success)
                {
                    log.WriteAsync($"ExecQuery Exception is: {MDb.exception.Message}", "Correct Password", 0);
                }
                else
                {
                    log.WriteAsync($"ExecQuery result is: {records.Depth} and {records.FieldCount}", "Correct Password", 3);
                }
            }

            using (MariaDB MDb = new("ProdDB", log))
            {
                log.WriteAsync("Executing a query via the overloaded ExecQuery method passing in the query directly", "Program", 3);
                MySqlConnector.MySqlDataReader records = MDb.ExecQuery("Select * from download_options");
                if (!MDb.success)
                {
                    log.WriteAsync($"ExecQuery Exception is: {MDb.exception.Message}", "Read Output", 0);
                }
                else
                {
                    log.WriteAsync($"ExecQuery result is: {records.Depth} and {records.FieldCount}", "Read Output", 3);
                }
                while (records.Read())
                {
                    log.WriteAsync($"Prov Name: {records["providername"].ToString().PadRight(30)}", "Read Output", 3);
                }
            }

            watch.Stop();
            log.WriteAsync($"Program executed in {watch.ElapsedMilliseconds} mSec", "Program", 1);
            log.Stop();
            Console.WriteLine("Done");
        }
    }
}
