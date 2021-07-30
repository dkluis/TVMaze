using DB_Lib;
using System;

namespace DataBase
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("DataBase Started");

            Console.WriteLine("Connection to the MariaDB Test-TVM-DB with wrong password");
            using (MariaDB MDb = new("server=ca-server.local; database=Test-TVM-DB; uid=dick; pwd=WrongPassword"))
            {
                Console.WriteLine("Opening the connection to the MariaDB Test-TVM-DB with wrong password");
                Console.WriteLine($"Connection to the MariaDB Test-TVM-DB with incorrect password and success code {MDb.success}");
                if (!MDb.success)
                {
                    Console.WriteLine($"Exception is: {MDb.exception.Message}");
                }
                Console.WriteLine("Closing the connection to the MariaDB Test-TVM-DB with wrong password");
            }

            using (MariaDB MDb = new())
            {
                Console.WriteLine("Opening the connection to the MariaDB Test-TVM-DB with correct password");
                if (!MDb.success)
                {
                    Console.WriteLine($"Open Exception is: {MDb.exception.Message}");
                }

                Console.WriteLine("Executing a query via the Command and default ExecQuery method");
                MDb.Command("Select * from key_values");
                if (!MDb.success)
                {
                    Console.WriteLine($"Command Exception is: {MDb.exception.Message}");
                }
                MySqlConnector.MySqlDataReader records = MDb.ExecQuery();
                if (!MDb.success)
                {
                    Console.WriteLine($"ExecQuery Exception is: {MDb.exception.Message}");
                }
                else
                {
                    Console.WriteLine($"ExecQuery result is: {records.Depth} and {records.FieldCount}");
                }
            }

            using (MariaDB MDb = new("ProdDB"))
            {
                Console.WriteLine("Executing a query via the overloaded ExecQuery method passing in the query directly");
                MySqlConnector.MySqlDataReader records = MDb.ExecQuery("Select * from download_options");
                if (!MDb.success)
                {
                    Console.WriteLine($"ExecQuery Exception is: {MDb.exception.Message}");
                }
                else
                {
                    Console.WriteLine($"ExecQuery result is: {records.Depth} and {records.FieldCount}");
                }
                while (records.Read())
                {
                    Console.WriteLine($"{records["providername"]}");
                }
            }

            Console.WriteLine("Done");
        }
    }
}
