using Common_Lib;
using DB_Lib;
using Entities_Lib;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Web_Lib;

namespace UpdateShowEpochs
{

    /// <summary>
    ///
    ///     1. Maintains the Last TvmShowID that was processed and evaluated (allows to ignore none followed shows from before the Last TvmShowId)
    ///     2. Gets from Tvmaze Web all Shows that were marked as having had a change (is also marked if an episode change happened)
    ///     3. Process
    ///         a. Ignores non followed shows below or equal to Last TvmShowId
    ///         b. Evaluates non followed shows (if it should be reviewed based on the review rules) that are above Last TvmShowId
    ///             i. Adds the shows that should be reviewed
    ///         c. Updates all followed shows and update and/or inserts its episodes if the epoch timestamp is not the same as it is in Tvmaze Local
    /// 
    /// </summary>
    class UpdateShowEpochs
    {
        static void Main()
        {
            string This_Program = "Update Show Epochs";
            Console.WriteLine($"{DateTime.Now}: {This_Program}");
            AppInfo appinfo = new("TVMaze", This_Program, "DbAlternate");
            TextFileHandler log = appinfo.TxtFile;
            log.Start();

            int showid;
            int showepoch;
            int LastEvaluatedShow;
            using (TvmCommonSql ge = new(appinfo)) { LastEvaluatedShow = ge.GetLastEvaluatedShow(); }
            int HighestShowId = LastEvaluatedShow;
            log.Write($"Last Evaluated ShowId = {LastEvaluatedShow}", "", 2);

            WebAPI tvmapi = new(appinfo);
            JObject jsoncontent = tvmapi.ConvertHttpToJObject(tvmapi.GetShowUpdateEpochs("day"));
            log.Write($"Found {jsoncontent.Count} updates on Tvmaze", This_Program, 2);

            Show tvmshow = new(appinfo);
            int indbepoch;

            foreach (KeyValuePair<string, JToken> show in jsoncontent)
            {
                showid = int.Parse(show.Key.ToString());
                showepoch = int.Parse(show.Value.ToString());
                using (TvmCommonSql gse = new(appinfo)) { indbepoch = gse.GetShowEpoch(showid); }
                if (showepoch == indbepoch) { log.Write($"Skipping {showid} since show is already up to date", "", 4); continue; }

                bool isEnded = false;
                using (TvmCommonSql isE = new(appinfo))
                {
                    isEnded = isE.IsShowIdEnded(showid);
                }
                tvmshow.FillViaTvmaze(showid);
                // if (tvmshow.TvmStatus == "Ended" && isEnded) { log.Write($"Show {tvmshow.ShowName} is (and already was) Ended - Skipping Update"); continue; }

                log.Write($"TvmShowId: {tvmshow.TvmShowId},  Name: {tvmshow.ShowName}; Tvmaze Epoch: {showepoch}, In DB Epoch {indbepoch}", "", 4);

                if (indbepoch == 0)
                {
                    using (MariaDB Mdbw = new(appinfo)) { Mdbw.ExecNonQuery($"insert into TvmShowUpdates values (0, {showid}, {showepoch}, '{DateTime.Now:yyyy-MM-dd}');"); Mdbw.Close(); }
                    log.Write($"Inserted Epoch Record {showid} {tvmshow.ShowName}", "", 3);
                    if (showid > LastEvaluatedShow)
                    {
                        if (showid > HighestShowId) { HighestShowId = showid; } 
                        if (!tvmshow.isForReview) { log.Write($"Show {showid} is rejected because of review rules {tvmshow.TvmUrl}"); continue; }
                    }
                    else
                    {
                        log.Write($"This show is evaluated already", "", 3); continue;
                    }
                    tvmshow.TvmStatus = "New";
                    tvmshow.isFollowed = false;
                    if (!tvmshow.DbInsert(false, "UpdateShowEpochs"))
                    {
                        log.Write($"Insert of Show {showid} Failed #############################", "", 0);
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
                                if (!eps.DbInsert()) { log.Write($"Episode Insert Failed {eps.TvmShowId} {eps.TvmEpisodeId} {eps.SeasonEpisode} #######################", "", 0); }
                                else { log.Write($"Inserted Episode {eps.TvmShowId}, {eps.ShowName}, {eps.TvmEpisodeId}, {eps.SeasonEpisode}", "", 3); }
                                idxepsbyshow++;
                            }
                        }
                        log.Write($"Number of Episodes for Show {showid}: {idxepsbyshow}", "", 2);
                    }
                }
                else
                {
                    using (MariaDB Mdbw = new(appinfo)) { Mdbw.ExecNonQuery($"update TvmShowUpdates set `TvmUpdateEpoch` = {show.Value}, `TvmUpdateDate` = '{DateTime.Now:yyyy-MM-dd}' where `TvmShowId` = {showid};"); Mdbw.Close(); }
                    if (!tvmshow.isDBFilled) { continue;  }
                    if (!tvmshow.DbUpdate())
                    {
                        log.Write($"Update of Show {showid} Failed ###################", "", 0);
                        using ( ActionItems ai = new(appinfo)) { ai.DbInsert($"Update of Show {showid} Failed"); }
                    }
                    else
                    {
                        int idxepsbyshow = 0;
                        using (EpisodesByShow epsbyshow = new())
                        {
                            List<Episode> ebs = epsbyshow.Find(appinfo, showid);
                            foreach (Episode eps in ebs)
                            {
                                log.Write($"Processing {eps.TvmShowId} {eps.TvmEpisodeId} {eps.SeasonEpisode}", "", 4);
                                if (!eps.isDBFilled)
                                {
                                    if (!eps.DbInsert())
                                    {
                                        log.Write($"Episode Insert Failed {eps.TvmShowId} {eps.TvmEpisodeId} {eps.SeasonEpisode} ##################", "", 0);
                                        using (ActionItems ai = new(appinfo)) { ai.DbInsert($"Episode Insert Failed {eps.TvmShowId} {eps.TvmEpisodeId} {eps.SeasonEpisode}"); }
                                    }
                                    else { log.Write($"Inserted Episode {eps.TvmShowId}, {eps.ShowName}, {eps.TvmEpisodeId}, {eps.SeasonEpisode}"); }
                                }
                                else
                                {
                                    if (!eps.DbUpdate())
                                    {
                                        log.Write($"Episode Update Failed {eps.TvmShowId} {eps.TvmEpisodeId} {eps.SeasonEpisode} ####################", "", 0);
                                        using (ActionItems ai = new(appinfo)) { ai.DbInsert($"Episode Update Failed {eps.TvmShowId} {eps.TvmEpisodeId} {eps.SeasonEpisode}"); }
                                    }
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

            using (TvmCommonSql se = new(appinfo)) { se.SetLastEvaluatedShow(HighestShowId); }

            log.Stop();
        }
    }
}