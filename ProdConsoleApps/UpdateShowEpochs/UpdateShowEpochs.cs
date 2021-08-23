using Common_Lib;
using DB_Lib;
using Entities_Lib;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Web_Lib;

namespace UpdateShowEpochs
{
    class UpdateShowEpochs
    {
        static void Main()
        {
            string This_Program = "Update Show Epochs";
            Console.WriteLine($"{DateTime.Now}: Starting {This_Program}");
            AppInfo appinfo = new("TVMaze", This_Program, "DbAlternate");

            Console.WriteLine($"{DateTime.Now}: Progress can be followed in {appinfo.FullPath}");
            TextFileHandler log = appinfo.TxtFile;
            log.Start();

            int showid;
            int showepoch;
            int LastEvaluatedShow;
            using (TvmCommonSql ge = new(appinfo)) { LastEvaluatedShow = ge.GetLastEvaluatedShow(); }
            log.Write($"Last Evaluated ShowId = {LastEvaluatedShow}");

            // Get the last 24 hours of Shows that changes on TVMaze
            WebAPI tvmapi = new(appinfo);
            JObject jsoncontent = tvmapi.ConvertHttpToJObject(tvmapi.GetShowUpdateEpochs("day"));
            log.Write($"Found {jsoncontent.Count} updates on Tvmaze", This_Program, 0);

            Show tvmshow = new(appinfo);

            int indbepoch;

            foreach (KeyValuePair<string, JToken> show in jsoncontent)
            {
                showid = int.Parse(show.Key.ToString());
                showepoch = int.Parse(show.Value.ToString());
                using (TvmCommonSql gse = new(appinfo)) { indbepoch = gse.GetShowEpoch(showid); }

                tvmshow.FillViaTvmaze(showid);
                if (showepoch == indbepoch) { log.Write($"Skipping since show is already up to date"); continue; }

                log.Write($"TvmShowId: {tvmshow.TvmShowId}, New Epoch: {showepoch}, In DB Epoch {indbepoch}, Name: {tvmshow.ShowName}");

                if (indbepoch == 0)
                {
                    using (MariaDB Mdbw = new(appinfo)) { Mdbw.ExecNonQuery($"insert into TvmShowUpdates values (0, {showid}, {showepoch}, '{DateTime.Now:yyyy-MM-dd}');"); Mdbw.Close(); }
                    // using (MariaDB Mdbw = new(appinfo)) { Mdbw.ExecNonQuery($"update TvmShowUpdates set `TvmUpdateEpoch` = {show.Value}, `TvmUpdateDate` = '{DateTime.Now:yyyy-MM-dd}' where `TvmShowId` = {showid};"); Mdbw.Close(); }

                    if (showid < LastEvaluatedShow) { log.Write($"This show is evaluated already"); continue; }

                    tvmshow.DbInsert();
                    using (TvmCommonSql se = new(appinfo)) { se.SetLastEvaluatedShow(showid); }
                    log.Write($"Inserted Epoch Record and Show Record");
                }
                else
                {
                    using (MariaDB Mdbw = new(appinfo)) { Mdbw.ExecNonQuery($"update TvmShowUpdates set `TvmUpdateEpoch` = {show.Value}, `TvmUpdateDate` = '{DateTime.Now:yyyy-MM-dd}' where `TvmShowId` = {showid};"); Mdbw.Close(); }
                    tvmshow.DbUpdate();
                    log.Write($"Updated Epoch Record and Show Record");
                }
                tvmshow.Reset();
            }

            log.Stop();
            Console.WriteLine($"{DateTime.Now}: Finished {This_Program} Program");
        }
    }
}