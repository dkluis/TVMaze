using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using MySqlConnector;

using Common_Lib;
using Web_Lib;
using DB_Lib;
using TvmEntities;

namespace UpdateShowEpochs
{
    class UpdateShowEpochs
    {
        static void Main()
        {
            string This_Program = "Update Show Epochs";
            Console.WriteLine($"Starting {This_Program}");
            AppInfo appinfo = new("TVMaze", This_Program, "DbAlternate");

            Console.WriteLine($"Progress can be followed in {appinfo.FullPath}");
            TextFileHandler log = appinfo.TxtFile;
            log.Start();

            MariaDB Mdbr = new(appinfo);
            MariaDB Mdbw = new(appinfo);
            MySqlDataReader rdr;
            Int32 showid;


            // Get the last 24 hours of Shows that changes on TVMaze
            WebAPI tvmapi = new(appinfo);
            JObject jsoncontent = tvmapi.ConvertHttpToJObject(tvmapi.GetShowUpdateEpochs("day"));
            log.Write($"Found {jsoncontent.Count} updates on Tvmaze", This_Program , 0);

            Show tvmshow = new(appinfo);

            foreach (KeyValuePair<string, JToken> show in jsoncontent)
            {
                //TODO figure out if this show's epoch is already up to date and continue to skip the rest
                showid = Int32.Parse(show.Key);

                tvmshow.FillViaTvmaze(showid);
                log.Write($"TvmShowId: {tvmshow.TvmShowId}, Epoch: {show.Value}, Name: {tvmshow.ShowName}");

                rdr = Mdbr.ExecQuery($"select * from TvmShowUpdates where `TvmShowId` = {showid}");
                if (!rdr.HasRows)
                {
                    Mdbw.ExecNonQuery($"insert into TvmShowUpdates values (0, {show.Key}, {show.Value}, '{DateTime.Now:yyyy-MM-dd}');");
                    if (!tvmshow.DbInsert()) { tvmshow.DbUpdate(); }
                    log.Write($"Inserted Epoch Record and Show Record");
                }
                else
                {
                    while (rdr.Read())
                    {
                        if (rdr["TvmUpdateEpoch"].ToString() != show.Value.ToString())
                        {
                            Mdbw.ExecNonQuery($"update TvmShowUpdates set `TvmUpdateEpoch` = {show.Value}, `TvmUpdateDate` = '{DateTime.Now:yyyy-MM-dd}' where `TvmShowId` = {showid};");
                            if (!tvmshow.DbInsert()) { tvmshow.DbUpdate();  }
                            log.Write($"Updated Epoch Record and Show Record");
                        }
                    }
                }
                // rdr.Close();
                Mdbr.Close();
                Mdbw.Close();
            }

            log.Stop();
            Console.WriteLine($"Finished {This_Program} Program");
        }
    }
}