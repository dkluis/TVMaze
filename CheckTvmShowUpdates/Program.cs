using Common_Lib;
using DB_Lib;
using MySqlConnector;
using Newtonsoft.Json.Linq;
using System;
using Web_Lib;

namespace CheckTvmShowUpdates
{
    class Program
    {
        static void Main()
        {
            Logger log = new("CheckTvmShowUpdates.log");
            log.Start("ImportFromPythonTVM");

            #region Testing Json Handling (New)

            #region JObject GetShow(1)

            /*
            using(WebAPI parsetest = new(log))
            {
                log.Write("Starting Parse test for Show 1 Info");

                JObject show = parsetest.ConvertHttpToJObject(parsetest.GetShow(1));
                log.Write($"Count is: {show.Count}");
                //log.Write($"{show}, {show.GetType()}");
                log.Write($"{show["id"]}, {show["url"]}, {show["genres"][0]}, {show["network"]["name"]}, {show["updated"]}");

            }
            */

            #endregion

            #region JArray GetFollowedShows()
            /*
            using (WebAPI testuserapi = new(log))
            {
                log.Write("Starting Followed Shows API test");
                JArray jsonc = testuserapi.ConvertHttpToJArray(testuserapi.GetFollowedShows());
                log.Write($"Count is: {jsonc.Count}");
                log.Write($"Type is: {jsonc.GetType()}");
                int idx = 1;
                foreach (var rec in jsonc)
                {
                    //log.Write($"Rec is: {rec}, Type is: {rec.GetType()}");
                    log.Write($"# in Array {idx} --> ShowId is: {rec["show_id"]}");
                    idx++;
                    if (idx > 5)
                    {
                        break;
                    }
                }
            }
            */
            #endregion

            #region GetShowUpdateEpochs("day")
            /*
            using (WebAPI testepochs = new(log))
            {
                log.Write("Starting Show Update Epoch API test");
                var jsonc = testepochs.ConvertHttpToJObject(testepochs.GetShowUpdateEpochs("day"));
                if (jsonc is not null)
                {
                    log.Write($"Count is: {jsonc.Count}");
                    log.Write($"Type is: {jsonc.GetType()}");
                    int idx = 1;
                    foreach (var rec in jsonc)
                    {
                        //log.Write($"Rec {idx} is: {rec}, Type is: {rec.GetType()}");
                        log.Write($"Showid {Int32.Parse(rec.Key)}, Epoch {rec.Value}");
                        idx++;
                        if (idx > 5)
                        {
                            break;
                        }
                    }
                }
            }
            */
            #endregion

            #region GetEpisodesByShow(1)
            /*
            using (WebAPI testshow = new(log))
            {
                log.Write("Starting Get Episodes by Show");
                JArray jsonc = testshow.ConvertHttpToJArray(testshow.GetEpisodesByShow(1));
                log.Write($"Count is: {jsonc.Count}");
                log.Write($"Type is: {jsonc.GetType()}");
                int idx = 1;
                foreach (var rec in jsonc)
                {
                    //log.Write($"Rec is: {rec}, Type is: {rec.GetType()}");
                    log.Write($"Show 1 Record {idx}: Episode {rec["id"]}, Url {rec["url"]}");
                    idx++;
                    if (idx > 5)
                    {
                        break;
                    }
                }
            }
            */
            #endregion

            #endregion

            #region Get TVMaze last 24 Show Updates and update Epoch table;

            WebAPI tvmapi = new(log);
            log.Write("Start to API test", "Program", 0);
            JObject jsoncontent = tvmapi.ConvertHttpToJObject(tvmapi.GetShowUpdateEpochs("day"));  //day or week or month
            log.Write($"Found {jsoncontent.Count} updates from Tvmaze in the last 24 hours", "Program", 0);

            MariaDB Mdbr = new("New-Test-DB", log);
            MariaDB Mdbw = new("New-Test-DB", log);
            MySqlDataReader rdr;

            //Get Last inserted Show ID
            Int32 LastShowInserted = 999999999;
            MariaDB lastShow = new("New-Test-DB", log);
            MySqlDataReader rlastshow;
            rlastshow = lastShow.ExecQuery($"select TvmShowId from `TvmDB-Test`.TvmShowUpdates order by TvmShowId desc limit 1;");
            while (rlastshow.Read())
            {
                LastShowInserted = Int32.Parse(rlastshow["TvmShowid"].ToString());
            }
            log.Write($"Last Show Inserted was {LastShowInserted}");
            lastShow.Close();

            bool InEpochTable;
            int idx = 1;

            foreach (var kvp in jsoncontent)
            {
                InEpochTable = false;

                //log.Write($"Record {idx}: {kvp}", "Looping Updated Shows", 0);

                rdr = Mdbr.ExecQuery($"select * from TvmShowUpdates where `TvmShowId` = {kvp.Key}");

                while (rdr.Read())
                {
                    if (rdr["TvmUpdateEpoch"].ToString() != kvp.Value.ToString())
                    {
                        Mdbw.ExecNonQuery($"update TvmShowUpdates set " +
                            $"`TvmUpdateEpoch` = {kvp.Value}, " +
                            $"`TvmUpdateDate` = '{DateTime.Now.ToString("yyyy-MM-dd")}' " +
                            $"where `Id` = {rdr["Id"]}");
                        Mdbw.Close();
                        log.Write($"Record {idx} Epoch {kvp.Value} updated for ShowID {kvp.Key}", "Looping Json", 3);
                        /*
                        // Show and Episodes should be updated (or inserted) if the show is being followed
                        HttpResponseMessage showinfo = tvmapi.GetShow(kvp.Key);
                        log.Write(showinfo.Content.ReadAsStringAsync().Result, "Update Show");
                        showinfo = tvmapi.GetEpisodesByShow(kvp.Key);
                        log.Write(showinfo.Content.ReadAsStringAsync().Result, "Update Episodes for Show");
                        */
                    }
                    else
                    {
                        log.Write($"Record {idx} No Action Epochs are the same for ShowID {kvp.Key}");
                    }
                    InEpochTable = true;
                }
                if (!InEpochTable)
                {
                    Mdbw.ExecNonQuery($"insert into TvmShowUpdates values (0, {kvp.Key}, {kvp.Value}, '{DateTime.Now.ToString("yyyy-MM-dd")}')");
                    Mdbw.Close();
                    if (Int32.Parse(kvp.Key) > LastShowInserted)
                    {
                        log.Write($"Record {idx} Inserted Epoch Record for {kvp.Key}", "Looping Json", 1);
                        //Show should be inserted with New status for review assuming it fits the selection rules
                    }
                }
                //if (kvp.Key > 20) { break; };
                Mdbr.Close();
                idx++;
            }

            tvmapi.Dispose();

            #endregion

            log.Stop();
        }
    }
}
