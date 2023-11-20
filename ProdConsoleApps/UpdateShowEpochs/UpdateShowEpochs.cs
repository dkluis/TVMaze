using System;
using Common_Lib;
using DB_Lib;
using DB_Lib.Tvmaze;
using Entities_Lib;
using Web_Lib;

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

        using TvmCommonSql ge                = new(appInfo);
        var                lastEvaluatedShow = ge.GetLastEvaluatedShow();

        var highestShowId = lastEvaluatedShow;
        log.Write($"Last Evaluated ShowId = {lastEvaluatedShow}", "", 2);

        using WebApi tvmApi      = new(appInfo);
        var          jsonContent = tvmApi.ConvertHttpToJObject(tvmApi.GetShowUpdateEpochs("day"));
        log.Write($"Found {jsonContent.Count} updates on Tvmaze", thisProgram, 2);

        Show tvmShow = new(appInfo);
        foreach (var show in jsonContent)
        {
            var                showId    = int.Parse(show.Key);
            var                showEpoch = int.Parse(show.Value!.ToString());
            using TvmCommonSql gse       = new(appInfo);
            if (gse.IsShowSkipping(showId)) continue;

            var inDbEpoch = gse.GetShowEpoch(showId);
            if (showEpoch == inDbEpoch)
            {
                log.Write($"Skipping {showId} since show is already up to date", "", 4);
                continue;
            }

            tvmShow.FillViaTvmaze(showId);
            log.Write(
                      $"TvmShowId: {tvmShow.TvmShowId},  Name: {tvmShow.ShowName}; Tvmaze Epoch: {showEpoch}, In DB Epoch {inDbEpoch}",
                      "", 4);

            if (inDbEpoch == 0)
            {
                using MariaDb mDbW = new(appInfo);
                mDbW.ExecNonQuery(
                                  $"insert into TvmShowUpdates values (0, {showId}, {showEpoch}, '{DateTime.Now:yyyy-MM-dd}');");
                mDbW.Close();

                log.Write($"Inserted Epoch Record {showId} {tvmShow.ShowName}");
                if (showId > lastEvaluatedShow)
                {
                    if (showId > highestShowId) highestShowId = showId;
                    if (!tvmShow.IsForReview)
                    {
                        log.Write($"Show {showId} is rejected because of review rules {tvmShow.TvmUrl}");
                        continue;
                    }
                } else
                {
                    log.Write("This show is evaluated already");
                    continue;
                }

                tvmShow.TvmStatus  = "New";
                tvmShow.IsFollowed = false;
                if (!tvmShow.DbInsert(false, "UpdateShowEpochs"))
                {
                    log.Write($"Insert of Show {showId} Failed #############################", "", 0);
                } else
                {
                    log.Write($"Inserted new Show {showId}, {tvmShow.ShowName}", "", 2);
                    var idxEpsByShow = 0;
                    using (EpisodesByShow epsByShow = new())
                    {
                        var ebs = epsByShow.Find(appInfo, showId);
                        foreach (var eps in ebs)
                        {
                            if (!eps.DbInsert())
                                log.Write(
                                          $"Episode Insert Failed {eps.TvmShowId} {eps.TvmEpisodeId} {eps.SeasonEpisode} #######################",
                                          "", 0);
                            else
                                log.Write(
                                          $"Inserted Episode {eps.TvmShowId}, {eps.ShowName}, {eps.TvmEpisodeId}, {eps.SeasonEpisode}");
                            idxEpsByShow++;
                        }
                    }

                    log.Write($"Number of Episodes for Show {showId}: {idxEpsByShow}", "", 2);
                }
            } else
            {
                using MariaDb mDbW = new(appInfo);
                mDbW.ExecNonQuery(
                                  $"update TvmShowUpdates set `TvmUpdateEpoch` = {show.Value}, `TvmUpdateDate` = '{DateTime.Now:yyyy-MM-dd}' where `TvmShowId` = {showId};");
                mDbW.Close();

                if (!tvmShow.IsDbFilled) continue;
                if (!tvmShow.DbUpdate())
                {
                    log.Write($"Update of Show {showId} Failed ###################", "", 0);
                    using ActionItems ai = new(appInfo);
                    ai.DbInsert($"Update of Show {showId} Failed");
                } else
                {
                    var                  idxEpsByShow = 0;
                    using EpisodesByShow epsByShow    = new();
                    var                  ebs          = epsByShow.Find(appInfo, showId);
                    foreach (var eps in ebs)
                    {
                        log.Write($"Processing {eps.TvmShowId} {eps.TvmEpisodeId} {eps.SeasonEpisode}", "", 4);
                        if (!eps.IsDbFilled)
                        {
                            if (!eps.DbInsert())
                            {
                                log.Write(
                                          $"Episode Insert Failed {eps.TvmShowId} {eps.TvmEpisodeId} {eps.SeasonEpisode} ##################",
                                          "", 0);
                                using ActionItems ai = new(appInfo);
                                ai.DbInsert(
                                            $"Episode Insert Failed {eps.TvmShowId} {eps.TvmEpisodeId} {eps.SeasonEpisode}");
                            } else
                            {
                                log.Write(
                                          $"Inserted Episode {eps.TvmShowId}, {eps.ShowName}, {eps.TvmEpisodeId}, {eps.SeasonEpisode}");
                            }
                        } else
                        {
                            if (!eps.DbUpdate())
                            {
                                log.Write(
                                          $"Episode Update Failed {eps.TvmShowId} {eps.TvmEpisodeId} {eps.SeasonEpisode} ####################",
                                          "", 0);
                                using ActionItems ai = new(appInfo);
                                ai.DbInsert(
                                            $"Episode Update Failed {eps.TvmShowId} {eps.TvmEpisodeId} {eps.SeasonEpisode}");
                            }
                        }

                        idxEpsByShow++;
                    }

                    log.Write($"Number of Episodes for Show {showId}: {idxEpsByShow}", "", 2);
                }

                log.Write($"Updated Show {showId}");
            }

            tvmShow.Reset();
        }

        using (TvmCommonSql se = new(appInfo))
        {
            se.SetLastEvaluatedShow(highestShowId);
        }

        log.Stop();
    }
}
