using Common_Lib;
using DB_Lib;
using System;

namespace ImportFromPythonTVM
{
    class Program
    {
        static void Main(string[] args)
        {

            Logger log = new("ImportFromPythonTVM.log");
            log.Start("ImportFromPythonTVM");

            MariaDB PythonDB = new("ProdDB", log);
            MariaDB DockerDB = new("C#DB", log);
            PythonDB.Command("select * from shows where `status` != 'Skipped' and `showid`;");
            MySqlConnector.MySqlDataReader presult = PythonDB.ExecQuery();
            while (presult.Read())
            {
                log.Write($"Processing: {presult["showid"].ToString().PadLeft(6)} : {presult["status"].ToString().PadRight(15)} : {presult["showname"].ToString()}", "Fill new Shows Table", 3);

                string showname = presult["showname"].ToString();
                if (showname.Contains("'"))
                {
                    showname = presult["showname"].ToString().Replace("'", "");
                }
                string premiered = presult["premiered"].ToString();
                if (premiered == "")
                {
                    premiered = "1970-01-01";
                }

                string isql = $"insert into Shows " +
                              //$"(`Id`,`TvmShowid`, `TvmStatus`, `TvmUrl`, `ShowName`, `ShowStatus`, `PremiereDate`, `Finder`, `CleanedShowName`, `AltShowName`, `UpdateDate`) " +
                              $"values (" +
                              $"0, " +
                              $"{presult["showid"]}, " +
                              $"'{presult["status"]}', " +
                              $"'{presult["url"]}', " +
                              $"'{showname}', " +
                              $"'{presult["showstatus"]}', " +
                              $"'{premiered}', " +
                              $"'{presult["download"]}', " +
                              $"default, " +
                              $"'{presult["alt_showname"]}', " +
                              $"default);";
                

                // string isql = $"Insert into TestInsert values (0, 'Test1');";
                log.Write(isql, "Sql Debug", 0);

                DockerDB.Command(isql);
                DockerDB.ExecNonQuery();
                if (!DockerDB.success)
                {
                    Environment.Exit(99);
                }
            }

            DockerDB.Command("select * from EpisodeInfo;");
            var dresult = DockerDB.ExecQuery();

            log.Start("ImportFromPythonTVM");
        }
    }
}
