using System;
using Common_Lib;
using Web_Lib;
using DB_Lib;
using System.Net.Http;
using Newtonsoft.Json;
using System.Collections.Generic;
using MySqlConnector;

namespace CheckTvmShowUpdates
{
    class Program
    {
        static void Main()
        {
            Logger log = new("CheckTvmShowUpdates.log");
            log.Start("ImportFromPythonTVM");

            WebAPI testuserapi = new(log);
            HttpResponseMessage testresult = testuserapi.GetFollowedShows();
            log.Write($"{testresult.Content.ReadAsStringAsync().Result}");
            Environment.Exit(999);



            #region Get TVMaze last 24 Show Updates;

            WebAPI tvmapi = new(log);
            log.Write("Start to API test", "Program", 0);
            HttpResponseMessage result = tvmapi.GetShowUpdateEpochs("day");  //day or week or month
            log.Write($"Result back from API call {result.StatusCode}", "Get Update Epochs", 3);

            var content = result.Content.ReadAsStringAsync().Result;
            var jsoncontent = JsonConvert.DeserializeObject<Dictionary<int, int>>(content);
            log.Write($"Found {jsoncontent.Count} updates from Tvmaze in the last 24 hours", "Program", 0);

            MariaDB Mdbr = new("New-Test-DB", log);
            //MariaDB Mdbse = new("New-Test-DB", log);
            MariaDB Mdbw = new("New-Test-DB", log);
            MySqlDataReader rdr;
            //MySqlDataReader rdse;
            bool InEpochTable;
            //bool InShowTable;
            //bool InEpisodeTable;

            foreach (var kvp in jsoncontent)
            {
                InEpochTable = false;

                //log.Write($"Key: {kvp.Key}, value: {kvp.Value}", "Looping Json", 0);
                rdr = Mdbr.ExecQuery($"select * from TvmShowUpdates where `TvmShowId` = {kvp.Key}");
                /*
                InShowTable = false;
                rdse = Mdbse.ExecQuery($"select * from Shows where `TvmShowId` = {kvp.Key}");
                while (rdse.Read())
                {
                    string tvmshowid = rdse["TvmShowID"].ToString();
                    if (kvp.Key == Int16.Parse(tvmshowid) ) { InShowTable = true; };
                }
                rdse.Close();
                */
                while (rdr.Read())
                {
                if (Int32.Parse(rdr["TvmUpdateEpoch"].ToString()) != kvp.Value)
                    {
                        Mdbw.ExecNonQuery($"update TvmShowUpdates set `TvmUpdateEpoch` = {kvp.Value} where `Id` = {rdr["Id"]}");
                        Mdbw.Close();
                        log.Write($"Epoch {kvp.Value} updated for ShowID {kvp.Key}", "Program", 3);
                        HttpResponseMessage showinfo = tvmapi.GetShow(kvp.Key);
                        log.Write(showinfo.Content.ReadAsStringAsync().Result, "Update Show");
                        showinfo = tvmapi.GetEpisodesByShow(kvp.Key);
                        log.Write(showinfo.Content.ReadAsStringAsync().Result, "Update Episodes for Show");
                        /*
                        // Update the Shows and Episode Tables if necessary
                        if (InShowTable)
                        {
                            // Do the Show Record update
                        }
                        // Loop through the episode and update or insert as necessary.
                        */
                    }
                    else
                    {
                        log.Write($"No Action Epochs are the same for ShowID {kvp.Key}");
                    }
                    InEpochTable = true;
                }
                if (!InEpochTable)
                {
                    Mdbw.ExecNonQuery($"insert into TvmShowUpdates values (0, {kvp.Key}, {kvp.Value})");
                    Mdbw.Close();
                    log.Write($"Inserted Epoch Record for {kvp.Key} need to create a record in shows with New status", "Program", 1);
                    HttpResponseMessage showinfo = tvmapi.GetShow(kvp.Key);
                    log.Write(showinfo.Content.ReadAsStringAsync().Result, "Insert Record Show");  // Insert a Record in shows with status New
                    showinfo = tvmapi.GetEpisodesByShow(kvp.Key);
                    log.Write(showinfo.Content.ReadAsStringAsync().Result, "Insert Episodes for Show"); // Insert all episode available for show.
                }
                //if (kvp.Key > 20) { break; };
                Mdbr.Close();
            }

            tvmapi.Dispose();



            #endregion

            log.Stop();
        }
    }
}
