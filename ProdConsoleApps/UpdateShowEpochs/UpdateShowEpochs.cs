using System;
using System.Collections.Generic;
using System.Text.Json;

using Common_Lib;

using DB_Lib;

using DB_Lib_EF.Entities;
using DB_Lib_EF.Models.MariaDB;

using DB_Lib.Tvmaze;

using Entities_Lib;

using Web_Lib;

using Show = Entities_Lib.Show;

namespace UpdateShowEpochs;

/// <summary>
///     1. Maintains the Last TvmShowID that was processed and evaluated (allows to ignore none followed shows from before
///     the Last TvmShowId)
///     2. Gets from Tvmaze Web all Shows that were marked as having had a change (is also marked if an episode change
///     happened)
///     3. Process
///     a. Ignores non followed shows below or equal to Last TvmShowId
///     b. Evaluates non followed shows (if it should be reviewed based on the review rules) that are above Last TvmShowId
///     i. Adds the shows that should be reviewed
///     c. Updates all followed shows and update and/or inserts its episodes if the epoch timestamp is not the same as it
///     is in Tvmaze Local
/// </summary>
internal static class UpdateShowEpochs
{
    private static void Main()
    {
        const string thisProgram = "Update Show Epochs";
        Console.WriteLine($"{DateTime.Now}: {thisProgram}");
        AppInfo appInfo = new("TVMaze", thisProgram, "DbAlternate");
        var     log     = appInfo.TxtFile;
        log.Start();
        LogModel.Start(thisProgram);

        using TvmCommonSql ge                = new(appInfo);
        var                lastEvaluatedShow = ge.GetLastEvaluatedShow();

        var highestShowId = lastEvaluatedShow;
        log.Write($"Last Evaluated ShowId = {lastEvaluatedShow}", "", 2);

        var logRec = new Log
                  {
                      RecordedDate = DateTime.Now,
                      Program      = thisProgram,
                      Function     = "Main",
                      Message      = $"Last Evaluated ShowId = {lastEvaluatedShow}",
                      Level        = 1,
                  };
        LogModel.Record(logRec);

        using WebApi tvmApi          = new(appInfo);
        var          updateResult    = tvmApi.GetShowUpdateEpochs("day");
        var          content         = updateResult.Content.ReadAsStringAsync().Result;
        var          updates         = JsonSerializer.Deserialize<SortedDictionary<int, int>>(content);

        if (updates == null)
        {
            log.Write($"Failed to retrieve updates from Tvmaze", thisProgram, 1);

            logRec = new Log
                     {
                         RecordedDate = DateTime.Now,
                         Program      = thisProgram,
                         Function     = "Main",
                         Message      = $"Failed to retrieve updates from Tvmaze",
                         Level        = 1,
                     };
            LogModel.Record(logRec);
        } else
        {
        //var          jsonContent  = tvmApi.ConvertHttpToJObject(tvmApi.GetShowUpdateEpochs("day"));

        log.Write($"Found {updates.Count} updates on Tvmaze", thisProgram, 2);

        logRec = new Log
                  {
                      RecordedDate = DateTime.Now,
                      Program      = thisProgram,
                      Function     = "Main",
                      Message      = $"Found {updates.Count} updates on Tvmaze",
                      Level        = 1,
                  };
        LogModel.Record(logRec);

        Show tvmShow = new(appInfo);

        foreach (var show in updates)
        {
            var                showId    = show.Key;
            var                showEpoch = show.Value;
            using TvmCommonSql gse       = new(appInfo);

            if (gse.IsShowSkipping(showId)) continue;

            var inDbEpoch = gse.GetShowEpoch(showId);

            if (showEpoch == inDbEpoch)
            {
                log.Write($"Skipping {showId} since show is already up to date", "", 4);

                logRec = new Log
                         {
                             RecordedDate = DateTime.Now,
                             Program      = thisProgram,
                             Function     = "Main",
                             Message      = $"Skipping {showId} since show is already up to date",
                             Level        = 5,
                         };
                LogModel.Record(logRec);

                continue;
            }

            tvmShow.FillViaTvmaze(showId);
            log.Write($"TvmShowId: {tvmShow.TvmShowId},  Name: {tvmShow.ShowName}: Tvmaze Epoch: {showEpoch}, In DB Epoch {inDbEpoch}", "", 4);

            logRec = new Log
                     {
                         RecordedDate = DateTime.Now,
                         Program      = thisProgram,
                         Function     = "Main",
                         Message      = $"TvmShowId: {tvmShow.TvmShowId},  Name: {tvmShow.ShowName}; Tvmaze Epoch: {showEpoch}, In DB Epoch {inDbEpoch}",
                         Level        = 4,
                     };
            LogModel.Record(logRec);

            if (inDbEpoch == 0)
            {
                using MariaDb mDbW = new(appInfo);
                mDbW.ExecNonQuery($"insert into TvmShowUpdates values (0, {showId}, {showEpoch}, '{DateTime.Now:yyyy-MM-dd}');");
                mDbW.Close();

                log.Write($"Inserted Epoch Record {showId} {tvmShow.ShowName}");

                logRec = new Log
                         {
                             RecordedDate = DateTime.Now,
                             Program      = thisProgram,
                             Function     = "Main",
                             Message      = $"Inserted Epoch Record {showId} {tvmShow.ShowName}",
                             Level        = 1,
                         };
                LogModel.Record(logRec);

                if (showId > lastEvaluatedShow)
                {
                    if (showId > highestShowId) highestShowId = showId;

                    if (!tvmShow.IsForReview)
                    {
                        log.Write($"Show {showId} is rejected because of review rules {tvmShow.TvmUrl}");

                        logRec = new Log
                                 {
                                     RecordedDate = DateTime.Now,
                                     Program      = thisProgram,
                                     Function     = "Main",
                                     Message      = $"Show {showId} {tvmShow.ShowName}is rejected because of review rules {tvmShow.TvmUrl}",
                                     Level        = 1,
                                 };
                        LogModel.Record(logRec);

                        continue;
                    }
                } else
                {
                    log.Write("This show is evaluated already");

                    logRec = new Log
                             {
                                 RecordedDate = DateTime.Now,
                                 Program      = thisProgram,
                                 Function     = "Main",
                                 Message      = $"This show {tvmShow.TvmShowId}: {tvmShow.ShowName} is evaluated already",
                                 Level        = 1,
                             };
                    LogModel.Record(logRec);

                    continue;
                }

                tvmShow.TvmStatus  = "New";
                tvmShow.IsFollowed = false;

                if (!tvmShow.DbInsert(false, "UpdateShowEpochs"))
                {
                    log.Write($"Insert of Show {showId} Failed #############################", "", 0);

                    logRec = new Log
                             {
                                 RecordedDate = DateTime.Now,
                                 Program      = thisProgram,
                                 Function     = "Main",
                                 Message      = $"Insert of Show {showId}: {tvmShow.ShowName} Failed #############################",
                                 Level        = 4,
                             };
                    LogModel.Record(logRec);
                } else
                {
                    log.Write($"Inserted new Show {showId}, {tvmShow.ShowName}", "", 2);

                    logRec = new Log
                             {
                                 RecordedDate = DateTime.Now,
                                 Program      = thisProgram,
                                 Function     = "Main",
                                 Message      = $"Inserted new Show {showId}, {tvmShow.ShowName}",
                                 Level        = 1,
                             };
                    LogModel.Record(logRec);
                    var idxEpsByShow = 0;

                    using (EpisodesByShow epsByShow = new())
                    {
                        var ebs = epsByShow.Find(appInfo, showId);

                        foreach (var eps in ebs)
                        {
                            if (!eps.DbInsert())
                            {
                                log.Write($"Episode Insert Failed {eps.TvmShowId} {eps.TvmEpisodeId} {eps.SeasonEpisode} #######################", "", 0);

                                logRec = new Log
                                         {
                                             RecordedDate = DateTime.Now,
                                             Program      = thisProgram,
                                             Function     = "Main",
                                             Message      = $"Episode Insert Failed {eps.TvmShowId} {eps.TvmEpisodeId} {eps.SeasonEpisode} #######################",
                                             Level        = 4,
                                         };
                                LogModel.Record(logRec);
                            } else
                            {
                                log.Write($"Inserted Episode {eps.TvmShowId}, {eps.ShowName}, {eps.TvmEpisodeId}, {eps.SeasonEpisode}");

                                logRec = new Log
                                         {
                                             RecordedDate = DateTime.Now,
                                             Program      = thisProgram,
                                             Function     = "Main",
                                             Message      = $"Inserted Episode {eps.TvmShowId}, {eps.ShowName}, {eps.TvmEpisodeId}, {eps.SeasonEpisode}",
                                             Level        = 1,
                                         };
                                LogModel.Record(logRec);
                            }

                            idxEpsByShow++;
                        }
                    }

                    log.Write($"Number of Episodes for Show {showId} {tvmShow.ShowName}: {idxEpsByShow}", "", 2);
                }
            } else
            {
                using MariaDb mDbW = new(appInfo);
                mDbW.ExecNonQuery($"update TvmShowUpdates set `TvmUpdateEpoch` = {show.Value}, `TvmUpdateDate` = '{DateTime.Now:yyyy-MM-dd}' where `TvmShowId` = {showId};");
                mDbW.Close();

                if (!tvmShow.IsDbFilled) continue;

                if (!tvmShow.DbUpdate())
                {
                    log.Write($"Update of Show {showId} Failed ###################", "", 0);

                    logRec = new Log
                             {
                                 RecordedDate = DateTime.Now,
                                 Program      = thisProgram,
                                 Function     = "Main",
                                 Message      = $"Update of Show {showId} Failed ###################",
                                 Level        = 4,
                             };
                    LogModel.Record(logRec);
                    ActionItemModel.RecordActionItem(thisProgram, $"Update of Show {showId} Failed", log);
                } else
                {
                    var                  idxEpsByShow = 0;
                    using EpisodesByShow epsByShow    = new();
                    var                  ebs          = epsByShow.Find(appInfo, showId);

                    foreach (var eps in ebs)
                    {
                        log.Write($"Processing {eps.TvmShowId} {eps.TvmEpisodeId} {eps.SeasonEpisode}", "", 4);

                        logRec = new Log
                                 {
                                     RecordedDate = DateTime.Now,
                                     Program      = thisProgram,
                                     Function     = "Main",
                                     Message      = $"Processing {eps.TvmShowId} {eps.TvmEpisodeId} {eps.SeasonEpisode}",
                                     Level        = 5,
                                 };
                        LogModel.Record(logRec);

                        if (!eps.IsDbFilled)
                        {
                            if (!eps.DbInsert())
                            {
                                log.Write($"Episode Insert Failed {eps.TvmShowId} {eps.TvmEpisodeId} {eps.SeasonEpisode} ##################", "", 0);

                                logRec = new Log
                                         {
                                             RecordedDate = DateTime.Now,
                                             Program      = thisProgram,
                                             Function     = "Main",
                                             Message      = $"Episode Insert Failed {eps.TvmShowId} {eps.TvmEpisodeId} {eps.SeasonEpisode} ##################",
                                             Level        = 4,
                                         };
                                LogModel.Record(logRec);
                                ActionItemModel.RecordActionItem(thisProgram, $"Episode Insert Failed {eps.TvmShowId} {eps.TvmEpisodeId} {eps.SeasonEpisode}", log);
                            } else
                            {
                                log.Write($"Inserted Episode {eps.TvmShowId}, {eps.ShowName}, {eps.TvmEpisodeId}, {eps.SeasonEpisode}");

                                logRec = new Log
                                         {
                                             RecordedDate = DateTime.Now,
                                             Program      = thisProgram,
                                             Function     = "Main",
                                             Message      = $"Inserted Episode {eps.TvmShowId}, {eps.ShowName}, {eps.TvmEpisodeId}, {eps.SeasonEpisode}",
                                             Level        = 1,
                                         };
                                LogModel.Record(logRec);
                            }
                        } else
                        {
                            if (!eps.DbUpdate())
                            {
                                log.Write($"Episode Update Failed {eps.TvmShowId} {eps.TvmEpisodeId} {eps.SeasonEpisode} ####################", "", 0);

                                logRec = new Log
                                         {
                                             RecordedDate = DateTime.Now,
                                             Program      = thisProgram,
                                             Function     = "Main",
                                             Message      = $"Episode Update Failed {eps.TvmShowId} {eps.TvmEpisodeId} {eps.SeasonEpisode} ####################",
                                             Level        = 4,
                                         };
                                LogModel.Record(logRec);
                                ActionItemModel.RecordActionItem(thisProgram, $"Episode Update Failed {eps.TvmShowId} {eps.TvmEpisodeId} {eps.SeasonEpisode}", log);
                            }
                        }

                        idxEpsByShow++;
                    }

                    log.Write($"Number of Episodes for Show {showId}: {idxEpsByShow}", "", 2);

                    logRec = new Log
                             {
                                 RecordedDate = DateTime.Now,
                                 Program      = thisProgram,
                                 Function     = "Main",
                                 Message      = $"Number of Episodes for Show {showId}: {idxEpsByShow}",
                                 Level        = 4,
                             };
                    LogModel.Record(logRec);
                }

                log.Write($"Updated Show {showId}");

                logRec = new Log
                         {
                             RecordedDate = DateTime.Now,
                             Program      = thisProgram,
                             Function     = "Main",
                             Message      = $"Updated Show {showId} {tvmShow.ShowName}",
                             Level        = 1,
                         };
                LogModel.Record(logRec);
            }

            tvmShow.Reset();
        }

        using (TvmCommonSql se = new(appInfo))
        {
            se.SetLastEvaluatedShow(highestShowId);
        }

        log.Stop();
        LogModel.Stop(thisProgram);
        }
    }
}
