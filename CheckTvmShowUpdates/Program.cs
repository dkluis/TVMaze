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
            AppInfo appinfo = new("CheckTvmShowUpd", "New-Test-DB", "CheckTvmShowUpdates.log");
            Logger log = appinfo.Log;
            log.Start();

            #region Testing Json Handling (New)

            #region JObject GetShow(1)
  
            using(WebAPI parsetest = new(log))
            {
                log.Write("Starting Parse test for Show 1 Info");

                JObject show = parsetest.ConvertHttpToJObject(parsetest.GetShow(1));
                log.Write($"Count is: {show.Count}");
                //log.Write($"{show}, {show.GetType()}");
                log.Write($"{show["id"]}, {show["url"]}, {show["genres"][0]}, {show["network"]["name"]}, {show["updated"]}");

            }
            
            #endregion

            #region JArray GetFollowedShows()
            
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
            
            #endregion

            #region GetShowUpdateEpochs("day")
            
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
            
            #endregion

            #region GetEpisodesByShow(1)
            
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
            
            #endregion

            #endregion

            #region Testing isShowFollowed

            bool isShowFollowed;
            using (TvmCommonSql ts = new(appinfo))
            {
                isShowFollowed = ts.IsShowIdFollowed(1);
            }
            log.Write($"Followed is {isShowFollowed}");
            using (TvmCommonSql ts = new(appinfo))
            {
                isShowFollowed = ts.IsShowIdFollowed(2);
            }
            log.Write($"Followed is {isShowFollowed}");

            #endregion

            #region Get TVMaze last 24 Show Updates and update Epoch table;

            #region Get the Epoch timestamp of tvmaze show updates in last 24 hours

            WebAPI tvmapi = new(log);
            log.Write("Start Get TVMaze last 24 Hour Updates", "Program", 0);
            JObject jsoncontent = tvmapi.ConvertHttpToJObject(tvmapi.GetShowUpdateEpochs("day"));  //day or week or month
            log.Write($"Found {jsoncontent.Count} updates from Tvmaze in the last 24 hours", "Program", 0);

            #endregion

            #region Get Last Show Evaluated for suitability from the Epoch Table

            Int32 LastShowInserted;
            using (TvmCommonSql ts = new(appinfo))
            {
                LastShowInserted = ts.GetLastTvmShowIdInserted();
            }
            if (LastShowInserted == 99999999)
            {
                log.Write($"No Last Show Inserted was found");
            }
            log.Write($"Last Show Inserted is {LastShowInserted}");

            #endregion

            #region Processing all Epoch updates

            MariaDB Mdbr = new("New-Test-DB", log);
            MariaDB Mdbw = new("New-Test-DB", log);
            MySqlDataReader rdr;

            bool InEpochTable;
            bool UpdateNeeded;
            foreach (var kvp in jsoncontent)
            {
                Int32 showid = Int32.Parse(kvp.Key);
                InEpochTable = false;
                UpdateNeeded = false;

                rdr = Mdbr.ExecQuery($"select * from TvmShowUpdates where `TvmShowId` = {showid}");
                while (rdr.Read())
                {
                    InEpochTable = true;
                    if (rdr["TvmUpdateEpoch"].ToString() != kvp.Value.ToString())
                    {
                        Mdbw.ExecNonQuery($"update TvmShowUpdates set " +
                            $"`TvmUpdateEpoch` = {kvp.Value}, " +
                            $"`TvmUpdateDate` = '{DateTime.Now:yyyy-MM-dd}' " +
                            $"where `Id` = {rdr["Id"]}");
                        Mdbw.Close();
                        log.Write($"Epoch {kvp.Value} updated for ShowID {showid}", "Looping Json", 3);
                        UpdateNeeded = true;
                    }
                    else
                    {
                        log.Write($"No Action Epochs are the same for ShowID {showid}");

                    }
                    InEpochTable = true;
                }
                Mdbr.Close();

                if (!InEpochTable)
                {
                    Mdbw.ExecNonQuery($"insert into TvmShowUpdates values (0, {showid}, {kvp.Value}, '{DateTime.Now:yyyy-MM-dd}')");
                    Mdbw.Close();
                    if (showid > LastShowInserted)
                    {
                        bool Followed;
                        using (TvmCommonSql ts = new(appinfo))
                        {
                            Followed = ts.IsShowIdFollowed(showid);
                        }
                        if (Followed)
                        {
                            //Figure out a way to delete episodes that don't exist anymore.
                            log.Write($"Here is where the update of Followed Shows and Episodes goes ShowId = {showid}");
                            using (WebAPI showinfo = new(log))
                            {
                                JObject show = showinfo.ConvertHttpToJObject(showinfo.GetShow(showid));
                                log.Write($"{show["id"]}, {show["url"]}, {show["updated"]}");
                                // Insert Show Sql
                            }
                            using (WebAPI episodes = new(log))
                            {
                                JArray jsonc = episodes.ConvertHttpToJArray(episodes.GetEpisodesByShow(showid));
                                foreach (var rec in jsonc)
                                {
                                    log.Write($"Show {showid}, Episode {rec["id"]}, Url {rec["url"]}");
                                    // Insert Episode Sql
                                }
                            }
                            continue;  // Done Processing this tvshow
                        }
                        log.Write($"Inserted Epoch Record for {showid}", "Looping Json", 1);
                        //Show should be inserted with New status for review assuming it fits the selection rules
                        using (WebAPI showinfo = new(log))
                        {
                            JObject show = showinfo.ConvertHttpToJObject(showinfo.GetShow(showid));
                            log.Write($"{show["id"]}, {show["url"]}, {show["updated"]}");
                            // Insert Show Sql
                        }
                        using (WebAPI episodes = new(log))
                        {
                            JArray jsonc = episodes.ConvertHttpToJArray(episodes.GetEpisodesByShow(showid));
                            foreach (var rec in jsonc)
                            {
                                log.Write($"Show {showid}, Episode {rec["id"]}, Url {rec["url"]}");
                                // Insert Episode Sql
                            }
                        }
                    }
                    else
                    {
                        if (UpdateNeeded)
                        {
                            bool Followed;
                            using (TvmCommonSql ts = new(appinfo))
                            {
                                Followed = ts.IsShowIdFollowed(showid);
                            }
                            if (Followed)
                            {
                                //Go update the Show info and all episodes
                                //Figure out a way to delete episodes that don't exist anymore.
                                log.Write($"Here is where the update of Followed Shows and Episodes goes ShowId = {showid}");

                            }
                        }
                    }
                }
                tvmapi.Dispose();
            }

            #endregion

            #endregion

            log.Stop();
        }
    }
}

