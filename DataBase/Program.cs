using DB_Lib;
using System;
using Log_Lib;

namespace DataBase
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("DataBase Started");

            Logger log = new();
            log.Write("Connection to the MariaDB Test-TVM-DB with wrong password");

            using (MariaDB MDb = new("server=ca-server.local; database=Test-TVM-DB; uid=dick; pwd=WrongPassword", log))
            {
                log.Write("Opening the connection to the MariaDB Test-TVM-DB with wrong password");
                log.Write($"Connection to the MariaDB Test-TVM-DB with incorrect password and success code {MDb.success}");
                if (!MDb.success)
                {
                    log.Write($"Exception is: {MDb.exception.Message}");
                }
                log.Write("Closing the connection to the MariaDB Test-TVM-DB with wrong password");
            }

            using (MariaDB MDb = new("", log))
            {
                log.Write("Opening the connection to the MariaDB Test-TVM-DB with correct password");
                if (!MDb.success)
                {
                    log.Write($"Open Exception is: {MDb.exception.Message}");
                }

                log.Write("Executing a query via the Command and default ExecQuery method");
                MDb.Command("Select * from key_values");
                if (!MDb.success)
                {
                    log.Write($"Command Exception is: {MDb.exception.Message}");
                }
                MySqlConnector.MySqlDataReader records = MDb.ExecQuery();
                if (!MDb.success)
                {
                    log.Write($"ExecQuery Exception is: {MDb.exception.Message}");
                }
                else
                {
                    log.Write($"ExecQuery result is: {records.Depth} and {records.FieldCount}");
                }
            }

            using (MariaDB MDb = new("ProdDB", log))
            {
                log.Write("Executing a query via the overloaded ExecQuery method passing in the query directly");
                MySqlConnector.MySqlDataReader records = MDb.ExecQuery("Select * from download_options");
                if (!MDb.success)
                {
                    log.Write($"ExecQuery Exception is: {MDb.exception.Message}");
                }
                else
                {
                    log.Write($"ExecQuery result is: {records.Depth} and {records.FieldCount}");
                }
                while (records.Read())
                {
                    log.Write($"{records["providername"]}");
                }
            }

            Console.WriteLine("Done");
        }
    }
}
