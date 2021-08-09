using Common_Lib;
using DB_Lib;
using System;

namespace ImportFromPythonTVM
{
    class Program
    {
        static void Main()
        {

            Logger log = new("ImportFromPythonTVM.log");
            log.Start("ImportFromPythonTVM");

            #region Migrate Shows
            /*
            using (MariaDB PythonDB = new("ProdDB", log))
            {
                
                MariaDB TvmazeDB = new("New-Test-DB", log);
                // PythonDB.Command("select * from shows where `status` != 'Skipped' and `showid` > 123;");
                PythonDB.Command("select * from shows where `status` != 'Skipped';");
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

                    TvmazeDB.Command(isql);
                    TvmazeDB.ExecNonQuery();
                    if (!TvmazeDB.success)
                    {
                        log.Write($"Skipping that Error and proceding with the next record ######################################################");
                    }
                }
            }
            */
            #endregion

            #region Migrate Episodes
            /*
            using (MariaDB PythonDB = new("ProdDB", log))
            {
                MariaDB TvmazeDB = new("New-Test-DB", log);
                //PythonDB.Command("select * from episodes where epiid = 13007;");
                PythonDB.Command("select * from episodes order by `showid`, `season`, `episode`;");
                MySqlConnector.MySqlDataReader presult = PythonDB.ExecQuery();
                while (presult.Read())
                {
                    string broadcastdate;
                    if (presult["airdate"] == DBNull.Value || presult["airdate"].ToString() == "")
                    {
                        broadcastdate = "NULL";
                    }
                    else
                    {
                        broadcastdate = presult["airdate"].ToString();
                        if (broadcastdate.Length > 10)
                        {
                            String[] bcd = broadcastdate.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                            broadcastdate = bcd[0].PadLeft(10, '0');
                            bcd = broadcastdate.Split("/", StringSplitOptions.RemoveEmptyEntries);
                            broadcastdate = "'" + bcd[2] + '-' + bcd[0] + "-" + bcd[1] + "'";
                        }
                    }

                    string plexdate;
                    if (presult["mystatus_date"] == DBNull.Value)
                    {
                        plexdate = "NULL";
                    }
                    else
                    {
                        plexdate = presult["mystatus_date"].ToString();
                        if (plexdate.Length > 10)
                        {
                            String[] pd = plexdate.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                            plexdate = pd[0].PadLeft(10, '0');
                            pd = plexdate.Split("/", StringSplitOptions.RemoveEmptyEntries);
                            plexdate = "'" + pd[2] + '-' + pd[0] + "-" + pd[1] + "'";
                        }
                    }

                    int episode;
                    if (presult["episode"] == DBNull.Value)
                    {
                        episode = 0;
                    }
                    else
                    {
                        if (presult["episode"].ToString() == " ")
                        {
                            episode = 0;
                        }
                        else
                        {
                            episode = Int16.Parse(presult["Episode"].ToString());
                        }
                    }

                    string isql = $"insert into Episodes " +
                        $"(`Id`, `TvmShowId`, `TvmEpisodeId`, `TvmUrl`, `SeasonEpisode`, `Season`, `Episode`, `BroadcastDate`, `PlexStatus`, `PlexDate`) " +
                        $"value " +
                        $"(0, " +
                        $"'{presult["showid"]}', " +
                        $"'{presult["epiid"]}', " +
                        $"'{presult["url"]}', " +
                        $"' ', " +
                        $"'{presult["season"]}', " +
                        $"'{episode}', " +
                        $"{broadcastdate}, " +
                        $"'{presult["mystatus"]}', " +
                        $"{plexdate});";

                    log.Write(isql, "Sql Debug", 0);

                    TvmazeDB.Command(isql);
                    TvmazeDB.ExecNonQuery();
                    if (!TvmazeDB.success)
                    {
                        Environment.Exit(99);
                    }
                }
            }
            */
            #endregion

            log.Stop();
        }
    }
}
