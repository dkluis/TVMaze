﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using Common_Lib;

using DB_Lib;

using Entities_Lib;

using Web_Lib;

namespace UpdatePlexAcquired;

internal static class UpdatePlexAcquired
{
    private static void Main()
    {
        const string thisProgram = "Update Plex Acquired";
        Console.WriteLine($"{DateTime.Now}: {thisProgram}");
        AppInfo appInfo = new("TVMaze", thisProgram, "DbAlternate");
        var     log     = appInfo.TxtFile;
        log.Start();

        // Read the Acquired Media txt file
        var plexAcquired = Path.Combine(appInfo.ConfigPath!, "Inputs", "PlexAcquired.log");

        if (!File.Exists(plexAcquired))
        {
            log.Write($"Plex Acquired Log File Does not Exist {plexAcquired}");
            log.Stop();
            Environment.Exit(0);
        }

        var acquired    = File.ReadAllLines(plexAcquired);
        var allAcquired = Path.Combine(appInfo.ConfigPath!, "Inputs", "AllAcquired.log");
        File.AppendAllLinesAsync(allAcquired, acquired);
        File.Delete(plexAcquired);
        log.Write($"Found {acquired.Length} records in {plexAcquired}");

        // Process all media lines
        foreach (var acq in acquired)
        {
            log.Write($"Processing acquired {acq}");
            var       acqInfo = Regex.Split(acq, "S[0-9]+E[0-9]+.", RegexOptions.IgnoreCase);
            var       acqSeas = Regex.Split(acq, "S[0-9]+.",        RegexOptions.IgnoreCase);
            string    show;
            var       episodeString = "";
            var       isSeason      = false;
            int       seasonNum;
            List<int> epsToUpdate = new();

            if (acqInfo.Length == 2)
            {
                show = acqInfo[0].Replace(".", " ").Trim();

                // ReSharper disable once StringLiteralTypo
                show          = show.Replace("WWW SCENETIME COM - ", "");
                episodeString = acq.Replace(acqInfo[1], "").Replace(acqInfo[0], "").Replace(".", " ").Trim();
                var seas = episodeString.ToLower().Split("e");
                seasonNum = int.Parse(seas[0].Replace("s", ""));
                var epiNum = int.Parse(seas[1]);

                // Special section to handle the Dexter versus Dexter New Blood season mix up on the internet.
                if (show.ToLower() == "dexter" && seasonNum > 8)
                {
                    show          =  "Dexter New Blood";
                    seasonNum     -= 8;
                    episodeString =  Common.BuildSeasonEpisodeString(seasonNum, epiNum);
                    log.Write("Changed Dexter to Dexter New Blood", "", 2);
                }

                //
                log.Write($"Found show {show} episode {episodeString}", "", 4);
            } else
            {
                if (acqSeas.Length == 2)
                {
                    isSeason  = true;
                    show      = acqSeas[0].Replace(".", " ").Trim();
                    seasonNum = int.Parse(acq.Replace(acqSeas[0], "").Replace(acqSeas[1], "").Replace(".", "").ToLower().Replace("s", ""));
                    log.Write($"Found the show's {show} whole season {seasonNum}", "", 4);
                } else
                {
                    log.Write($"Could not find a show and episode for {acq}, is probably a movie or music #################", "", 2);
                    using MediaFileHandler mfh = new(appInfo);
                    mfh.MoveNonTvMediaToPlex(acq);

                    continue;
                }
            }

            SearchShowsViaNames showToUpdate = new();
            var                 showId       = showToUpdate.Find(appInfo, show);

            if (showId.Count != 1)
            {
                log.Write($"Could not determine ShowId for: {show}, found {showId.Count} records", "", 2);

                if (showId.Count == 0)
                {
                    var reducedShow         = Common.RemoveSuffixFromShowName(show);
                    var reducedShowToUpdate = showToUpdate.Find(appInfo, reducedShow);

                    if (reducedShowToUpdate.Count == 1)
                    {
                        log.Write($"Found {reducedShow} trying this one", "", 2);
                        showId = reducedShowToUpdate;
                    } else
                    {
                        using ActionItems ai = new(appInfo);
                        ai.DbInsert($"Could not determine ShowId for: {show}, found {showId.Count} records");

                        continue;
                    }
                }
            }

            var epiId = 0;

            if (!isSeason)
            {
                EpisodeSearch episodeToUpdate = new();
                epiId = episodeToUpdate.Find(appInfo, int.Parse(showId[0].ToString()), episodeString);
                epsToUpdate.Add(epiId);
                log.Write($"Working on ShowId {showId[0]} and EpisodeId {epiId}", "", 4);
            }

            Show foundShow = new(appInfo);

            switch (isSeason)
            {
                case false when epiId == 0:
                {
                    log.Write($"Could not find episode for Show {show} and Episode String {episodeString}", "", 2);

                    using (ActionItems ai = new(appInfo))
                    {
                        ai.DbInsert($"Could not find episode for Show {show} and Episode String {episodeString}");
                    }

                    foundShow.FillViaTvmaze(showId[0]);
                    using MediaFileHandler mfh = new(appInfo);
                    mfh.MoveMediaToPlex(acq, null, foundShow, seasonNum);
                    foundShow.Reset();

                    continue;
                }

                case true:
                {
                    using MariaDb mDbE = new(appInfo);
                    var           rdrE = mDbE.ExecQuery($"select TvmEpisodeId from Episodes where `TvmShowId` = {showId[0]} and `Season` = {seasonNum}");
                    while (rdrE.Read()) epsToUpdate.Add(int.Parse(rdrE[0].ToString()!));

                    break;
                }
            }

            if (!isSeason)
            {
                using Episode epiToUpdate = new(appInfo);
                epiToUpdate.FillViaTvmaze(epiId);

                if (epiToUpdate.PlexStatus != " ")
                {
                    log.Write($"Not updating Tvmaze status already is {epiToUpdate.PlexStatus} on {epiToUpdate.PlexDate}", "", 2);
                } else
                {
                    using WebApi uts = new(appInfo);
                    uts.PutEpisodeToAcquired(epiToUpdate.TvmEpisodeId);
                    epiToUpdate.PlexStatus = "Acquired";
                    epiToUpdate.PlexDate   = DateTime.Now.ToString("yyyy-MM-dd");
                }

                if (!epiToUpdate.DbUpdate())
                    log.Write($"Error Updating Episode {epiToUpdate.TvmEpisodeId}", "", 0);

                using MediaFileHandler mfh = new(appInfo);
                mfh.MoveMediaToPlex(acq, epiToUpdate);
            } else
            {
                Episode firstEpi = new(appInfo);
                firstEpi.FillViaTvmaze(epsToUpdate[0]);

                foreach (var epi in epsToUpdate)
                {
                    Episode? epiToUpdate = new(appInfo);

                    if (firstEpi.TvmEpisodeId != epi)
                        epiToUpdate.FillViaTvmaze(epi);
                    else
                        epiToUpdate = firstEpi;

                    if (epiToUpdate.PlexStatus != " ")
                    {
                        log.Write($"Not updating Tvmaze status already is {epiToUpdate.PlexStatus} on {epiToUpdate.PlexDate}", "", 2);
                    } else
                    {
                        using WebApi uts = new(appInfo);
                        uts.PutEpisodeToAcquired(epiToUpdate.TvmEpisodeId);
                        epiToUpdate.PlexStatus = "Acquired";
                        epiToUpdate.PlexDate   = DateTime.Now.ToString("yyyy-MM-dd");
                    }

                    if (!epiToUpdate.DbUpdate())
                        log.Write($"Error Updating Episode {epiToUpdate.TvmEpisodeId}", "", 0);
                }

                using MediaFileHandler mfh = new(appInfo);
                mfh.MoveMediaToPlex(acq, firstEpi);
            }
        }

        log.Stop();
    }
}
