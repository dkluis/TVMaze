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
    class UpdatePlexAcquired
    {
        static void Main(string[] args)
        {
            string This_Program = "Update Plex Acquired";
            Console.WriteLine($"{DateTime.Now}: {This_Program} Started");
            AppInfo appinfo = new("TVMaze", This_Program, "DbAlternate");
            Console.WriteLine($"{DateTime.Now}: {This_Program} Progress can be followed in {appinfo.FullPath}");
            TextFileHandler log = appinfo.TxtFile;
            log.Start();

            //string PlexAcquired = Path.Combine(appinfo.ConfigPath, "Inputs", "PlexAcquired.log");  TODO Fix the hardcoding below...
            string PlexAcquired = "/Volumes/HD-Data-CA-Server/PlexMedia/PlexProcessing/TVMaze/Logs/PlexAcquired.log";
            if (!File.Exists(PlexAcquired))
            {
                log.Write($"Plex Acquired Log File Does not Exist {PlexAcquired}");
                log.Stop();
                Console.WriteLine($"{DateTime.Now}: {This_Program} Finished");
                Environment.Exit(0);
            }

            // TODO add whole season processing

            string[] acquired = File.ReadAllLines(PlexAcquired);
            string AllAcquired = Path.Combine(appinfo.ConfigPath, "Inputs", "AllAcquired.log");
            File.AppendAllLinesAsync(AllAcquired, acquired);
            File.Delete(PlexAcquired);
            log.Write($"Found {acquired.Length} records in {PlexAcquired}");

            // Process each acquisition (either directory or file)
            foreach (string acq in acquired)
            {
                log.Write($"Processing acquired {acq}");
                string[] acqinfo = Regex.Split(acq, "S[0-9]+E[0-9]+.", RegexOptions.IgnoreCase);
                string[] acqseas = Regex.Split(acq, "S[0-9]+.", RegexOptions.IgnoreCase);
                string show = "";
                string episode = "";
                bool isSeason = false;
                int season = 99;
                if (acqinfo.Length == 2)
                {
                    show = acqinfo[0].Replace(".", " ").Trim();
                    episode = acq.Replace(acqinfo[1], "").Replace(acqinfo[0], "").Replace(".", " ").Trim();
                    string[] seas = episode.ToLower().Split("e");
                    season = int.Parse(seas[0].ToString().Replace("s", ""));
                    log.Write($"Found show {show} episode {episode}, season {season}", "", 4);
                }
                else 
                {
                    if (acqseas.Length == 2)
                    {
                        isSeason = true;
                        show = acqseas[0].Replace(".", " ").Trim();
                        season = int.Parse(acqseas[0].ToString().ToLower().Replace("s", ""));
                        log.Write($"Found the show's {show} whole season {season}", "", 4);
                    }
                    else
                    {
                        log.Write($"Could not find a show and episode for {acq}, is probably a movie #################", "", 2); continue;
                    }
                }

                SearchShowsViaNames showtoupdate = new();
                List<int> showid = showtoupdate.Find(appinfo, show);
                if (showid.Count != 1)
                {
                    log.Write($"Could not determine ShowId for: {show}, found {showid.Count} records", "", 2);
                    using (ActionItems ai = new(appinfo)) { ai.DbInsert($"Could not determine ShowId for: {show}, found {showid.Count} records"); }
                    continue;
                }
                //TODO process all episodes for a season of a show.
                int epiid = 0;
                if (!isSeason)
                {
                    EpisodeSearch episodetoupdate = new();
                    epiid = episodetoupdate.Find(appinfo, int.Parse(showid[0].ToString()), episode);
                    log.Write($"Working on ShowId {showid[0]} and EpisodeId {epiid}");
                }

                if (epiid == 0)
                {
                    log.Write($"Could not find episode for Show {show} and Episode String {episode}", "", 2);
                    using (ActionItems ai = new(appinfo)) { ai.DbInsert($"Could not find episode for Show {show} and Episode String {episode}"); }
                    Show foundshow = new(appinfo);
                    foundshow.FillViaTvmaze(showid[0]);
                    using (MediaFileHandler mfh = new(appinfo)) { mfh.MoveMediaToPlex(acq, null, foundshow, season); }
                    foundshow.Reset();
                    continue;
                }
                using (Episode epitoupdate = new(appinfo))
                {
                    epitoupdate.FillViaTvmaze(epiid);
                    if (epitoupdate.PlexStatus != " ") { log.Write($"Not updating Tvmaze {epitoupdate.TvmEpisodeId} status already is {epitoupdate.PlexStatus} on {epitoupdate.PlexDate}", "", 2); }

                    using (WebAPI uts = new(appinfo)) { uts.PutEpisodeToAcquired(epitoupdate.TvmEpisodeId); }
                    epitoupdate.PlexStatus = "Acquired";
                    epitoupdate.PlexDate = DateTime.Now.ToString("yyyy-MM-dd");
                    if (!epitoupdate.DbUpdate()) { log.Write($"Error Updating Episode {epitoupdate.TvmEpisodeId}", "", 0); }

                    using (MediaFileHandler mfh = new(appinfo))
                    {
                        mfh.MoveMediaToPlex(acq, epitoupdate);
                    }
                }
            }

            log.Stop();
            Console.WriteLine($"{DateTime.Now}: {This_Program} Finished");
        }
    }
}