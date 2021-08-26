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
            Console.WriteLine($"{DateTime.Now}: {This_Program} Started");
            AppInfo appinfo = new("TVMaze", This_Program, "DbAlternate");

            Console.WriteLine($"{DateTime.Now}: {This_Program} Progress can be followed in {appinfo.FullPath}");
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
                if (showepoch == indbepoch) { log.Write($"Skipping since show is already up to date", "", 4); continue; }

                tvmshow.FillViaTvmaze(showid);
                log.Write($"TvmShowId: {tvmshow.TvmShowId},  Name: {tvmshow.ShowName}; Tvmaze Epoch: {showepoch}, In DB Epoch {indbepoch}", "", 4);

                if (indbepoch == 0)
                {
                    using (MariaDB Mdbw = new(appinfo)) { Mdbw.ExecNonQuery($"insert into TvmShowUpdates values (0, {showid}, {showepoch}, '{DateTime.Now:yyyy-MM-dd}');"); Mdbw.Close(); }
                    log.Write($"Inserted Epoch Record {showid} {tvmshow.ShowName}", "", 4);
                    if (showid > LastEvaluatedShow)
                    {
                        using (TvmCommonSql se = new(appinfo)) { se.SetLastEvaluatedShow(showid); }
                        if (!tvmshow.isForReview) { log.Write($"Show {showid} is rejected because of review rules {tvmshow.TvmUrl}"); continue; }
                    }
                    else
                    {
                        log.Write($"This show is evaluated already", "", 4); continue;
                    }
                    //if (!tvmshow.isDBFilled) { log.Write($"Show {showid} is not a followed show", "", 3); continue; }
                    if (!tvmshow.DbInsert())
                    {
                        log.Write($"Insert of Show {showid} Failed", "", 2);
                    }
                    else
                    {
                        log.Write($"Inserted new Show {showid}, {tvmshow.ShowName}", "", 2);
                        int idxepsbyshow = 0;
                        using (EpisodesByShow epsbyshow = new())
                        {
                            List<Episode> ebs = epsbyshow.Find(appinfo, showid);
                            foreach (Episode eps in ebs)
                            {
                                if (!eps.DbInsert()) { log.Write($"Episode Insert Failed {eps.TvmShowId} {eps.TvmEpisodeId} {eps.SeasonEpisode}"); }
                                else { log.Write($"Inserted Episode {eps.TvmShowId}, {eps.ShowName}, {eps.TvmEpisodeId}, {eps.SeasonEpisode}", "", 4); }
                                idxepsbyshow++;
                            }
                        }
                        log.Write($"Number of Episodes for Show {showid}: {idxepsbyshow}", "", 2);
                    }
                }
                else
                {
                    using (MariaDB Mdbw = new(appinfo)) { Mdbw.ExecNonQuery($"update TvmShowUpdates set `TvmUpdateEpoch` = {show.Value}, `TvmUpdateDate` = '{DateTime.Now:yyyy-MM-dd}' where `TvmShowId` = {showid};"); Mdbw.Close(); }
                    if (!tvmshow.isDBFilled) { log.Write($"Show {showid} is not a followed show", "", 3); continue;  }
                    if (!tvmshow.DbUpdate())
                    {
                        log.Write($"Update of Show {showid} Failed", "", 2);
                    }
                    else
                    {
                        //TODO Update and Insert the Episode
                        int idxepsbyshow = 0;
                        using (EpisodesByShow epsbyshow = new())
                        {
                            List<Episode> ebs = epsbyshow.Find(appinfo, showid);
                            foreach (Episode eps in ebs)
                            {
                                if (!eps.isDBFilled)
                                {
                                    if (!eps.DbInsert()) { log.Write($"Episode Insert Failed {eps.TvmShowId} {eps.TvmEpisodeId} {eps.SeasonEpisode}", "", 0); }
                                    else { log.Write($"Inserted Episode {eps.TvmShowId}, {eps.ShowName}, {eps.TvmEpisodeId}, {eps.SeasonEpisode}"); }
                                }
                                else
                                {
                                    //TODO create DBUpdate for episodes
                                    log.Write($"Should be Updating {eps.TvmShowId}, {eps.ShowName}, {eps.TvmEpisodeId}, {eps.SeasonEpisode}", "", 2); 
                                }
                                idxepsbyshow++;
                            }
                        }
                        log.Write($"Number of Episodes for Show {showid}: {idxepsbyshow}", "", 2);
                    }
                    log.Write($"Updated Show {showid}");

                }
                tvmshow.Reset();
            }

            log.Stop();
            Console.WriteLine($"{DateTime.Now}: {This_Program} Finished");
        }
    }
}