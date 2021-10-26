using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Common_Lib;
using DB_Lib;
using Entities_Lib;
using Web_Lib;

namespace UpdatePlexAcquired
{
    internal class UpdatePlexAcquired
    {
        private static void Main(string[] args)
        {
            var This_Program = "Update Plex Acquired";
            Console.WriteLine($"{DateTime.Now}: {This_Program}");
            AppInfo appinfo = new("TVMaze", This_Program, "DbAlternate");
            var log = appinfo.TxtFile;
            log.Start();

            var PlexAcquired = Path.Combine(appinfo.ConfigPath, "Inputs", "PlexAcquired.log");
            if (!File.Exists(PlexAcquired))
            {
                log.Write($"Plex Acquired Log File Does not Exist {PlexAcquired}");
                log.Stop();
                Environment.Exit(0);
            }

            var acquired = File.ReadAllLines(PlexAcquired);
            var AllAcquired = Path.Combine(appinfo.ConfigPath, "Inputs", "AllAcquired.log");
            File.AppendAllLinesAsync(AllAcquired, acquired);
            File.Delete(PlexAcquired);
            log.Write($"Found {acquired.Length} records in {PlexAcquired}");

            foreach (var acq in acquired)
            {
                log.Write($"Processing acquired {acq}");
                var acqinfo = Regex.Split(acq, "S[0-9]+E[0-9]+.", RegexOptions.IgnoreCase);
                var acqseas = Regex.Split(acq, "S[0-9]+.", RegexOptions.IgnoreCase);
                var show = "";
                var episode = "";
                var isSeason = false;
                var season = 99;
                List<int> epstoupdate = new();
                if (acqinfo.Length == 2)
                {
                    show = acqinfo[0].Replace(".", " ").Trim();
                    episode = acq.Replace(acqinfo[1], "").Replace(acqinfo[0], "").Replace(".", " ").Trim();
                    var seas = episode.ToLower().Split("e");
                    season = int.Parse(seas[0].Replace("s", ""));
                    log.Write($"Found show {show} episode {episode}, season {season}", "", 4);
                }
                else
                {
                    if (acqseas.Length == 2)
                    {
                        isSeason = true;
                        show = acqseas[0].Replace(".", " ").Trim();
                        season = int.Parse(acq.Replace(acqseas[0], "").Replace(acqseas[1], "").Replace(".", "")
                            .ToLower().Replace("s", ""));
                        //season = int.Parse(acqseas[0].ToString().ToLower().Replace("s", ""));
                        log.Write($"Found the show's {show} whole season {season}", "", 4);
                    }
                    else
                    {
                        log.Write(
                            $"Could not find a show and episode for {acq}, is probably a movie or music #################",
                            "", 2);
                        using (MediaFileHandler mfh = new(appinfo))
                        {
                            mfh.MoveNonTvMediaToPlex(acq);
                        }

                        continue;
                    }
                }

                SearchShowsViaNames showtoupdate = new();
                var showid = showtoupdate.Find(appinfo, show);
                if (showid.Count != 1)
                {
                    log.Write($"Could not determine ShowId for: {show}, found {showid.Count} records", "", 2);
                    if (showid.Count == 0)
                    {
                        var reducedShow = Common.RemoveSuffixFromShowName(show);
                        var reducedShowToUpdate = showtoupdate.Find(appinfo, reducedShow);
                        if (reducedShowToUpdate.Count == 1)
                        {
                            log.Write($"Found {reducedShow} trying this one", "", 2);
                            showid = reducedShowToUpdate;
                        }
                        else
                        {
                            using (ActionItems ai = new(appinfo))
                            {
                                ai.DbInsert($"Could not determine ShowId for: {show}, found {showid.Count} records");
                            }

                            continue;
                        }
                    }
                }

                var epiid = 0;
                if (!isSeason)
                {
                    EpisodeSearch episodetoupdate = new();
                    epiid = episodetoupdate.Find(appinfo, int.Parse(showid[0].ToString()), episode);
                    epstoupdate.Add(epiid);
                    log.Write($"Working on ShowId {showid[0]} and EpisodeId {epiid}", "", 4);
                }

                if (!isSeason && epiid == 0)
                {
                    log.Write($"Could not find episode for Show {show} and Episode String {episode}", "", 2);
                    using (ActionItems ai = new(appinfo))
                    {
                        ai.DbInsert($"Could not find episode for Show {show} and Episode String {episode}");
                    }

                    Show foundshow = new(appinfo);
                    foundshow.FillViaTvmaze(showid[0]);
                    using (MediaFileHandler mfh = new(appinfo))
                    {
                        mfh.MoveMediaToPlex(acq, null, foundshow, season);
                    }

                    foundshow.Reset();
                    continue;
                }

                if (isSeason)
                    using (MariaDB Mdbe = new(appinfo))
                    {
                        var rdre = Mdbe.ExecQuery(
                            $"select TvmEpisodeId from Episodes where `TvmShowId` = {showid[0]} and `Season` = {season}");
                        while (rdre.Read()) epstoupdate.Add(int.Parse(rdre[0].ToString()));
                    }

                if (!isSeason)
                {
                    using (Episode epitoupdate = new(appinfo))
                    {
                        epitoupdate.FillViaTvmaze(epiid);
                        if (epitoupdate.PlexStatus != " ")
                        {
                            log.Write(
                                $"Not updating Tvmaze status already is {epitoupdate.PlexStatus} on {epitoupdate.PlexDate}",
                                "", 2);
                        }
                        else
                        {
                            using (WebAPI uts = new(appinfo))
                            {
                                uts.PutEpisodeToAcquired(epitoupdate.TvmEpisodeId);
                            }

                            epitoupdate.PlexStatus = "Acquired";
                            epitoupdate.PlexDate = DateTime.Now.ToString("yyyy-MM-dd");
                        }

                        if (!epitoupdate.DbUpdate())
                            log.Write($"Error Updating Episode {epitoupdate.TvmEpisodeId}", "", 0);

                        using (MediaFileHandler mfh = new(appinfo))
                        {
                            mfh.MoveMediaToPlex(acq, epitoupdate);
                        }
                    }
                }
                else
                {
                    Episode firstEpi = new(appinfo);
                    firstEpi.FillViaTvmaze(epstoupdate[0]);
                    foreach (var epi in epstoupdate)
                    {
                        Episode eptoupdate = new(appinfo);
                        if (firstEpi.TvmEpisodeId != epi)
                            eptoupdate.FillViaTvmaze(epi);
                        else
                            eptoupdate = firstEpi;
                        if (eptoupdate.PlexStatus != " ")
                        {
                            log.Write(
                                $"Not updating Tvmaze status already is {eptoupdate.PlexStatus} on {eptoupdate.PlexDate}",
                                "", 2);
                        }
                        else
                        {
                            using (WebAPI uts = new(appinfo))
                            {
                                uts.PutEpisodeToAcquired(eptoupdate.TvmEpisodeId);
                            }

                            eptoupdate.PlexStatus = "Acquired";
                            eptoupdate.PlexDate = DateTime.Now.ToString("yyyy-MM-dd");
                        }

                        if (!eptoupdate.DbUpdate())
                            log.Write($"Error Updating Episode {eptoupdate.TvmEpisodeId}", "", 0);
                    }

                    using (MediaFileHandler mfh = new(appinfo))
                    {
                        mfh.MoveMediaToPlex(acq, firstEpi);
                    }
                }
            }

            log.Stop();
        }
    }
}