using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;

using Common_Lib;
using DB_Lib;

using MySqlConnector;

namespace Entities_Lib
{
    public class MediaFileHandler : IDisposable
    {
        public string PlexMediaTvShows;
        public string PlexMediaTvShowSeries;
        public string PlexMediaKidsTvShows;
        // public string PlexMediaMovies;
        // public string PlexMediaMovieSeries;
        // public string PlexMediaKidsMovies;
        public string PlexMediaAcquire;

        public List<string> TvShowsInSeries;
        public List<string> MoviesinSeries;
        public List<string> TvShowsInKids;

        private readonly MariaDB Mdb;
        private readonly TextFileHandler log;
        private readonly AppInfo Appinfo;

        public MediaFileHandler(AppInfo appinfo)
        {
            Appinfo = appinfo;
            Mdb = new(appinfo);
            log = appinfo.TxtFile;

            GetSetMediaInfo();

        }

        public void GetSetMediaInfo()
        {
            PlexMediaTvShows = GetDirectoryViaMediaType("TS");
            PlexMediaTvShowSeries = GetDirectoryViaMediaType("TSS");
            PlexMediaKidsTvShows = GetDirectoryViaMediaType("KTS");
            PlexMediaAcquire = GetDirectoryViaMediaType("ACQ");
        }

        public string GetDirectoryViaMediaType(string mt)
        {
            string path = "";
            MySqlDataReader rdr = Mdb.ExecQuery($"select `PlexLocation` from `MediaTypes` where `MediaType` = '{mt}'");
            if (rdr is not null)
            {
                while (rdr.Read())
                {
                    path = rdr[0].ToString();
                }
            }
            Mdb.Close();
            return path;
        }

        public bool DeleteEpisodeFiles(Episode epi)
        {
            if (!epi.isAutoDelete) { return true; }
            bool success = false;

            string directory = "";
            switch (epi.MediaType)
            {
                case "TS":
                    directory = PlexMediaTvShows;
                    break;
                case "TSS":
                    directory = PlexMediaTvShowSeries;
                    break;
                case "KTS":
                    directory = PlexMediaKidsTvShows;
                    break;
                default:
                    break;
            }

            string seas = $"Season {epi.SeasonNum}";
            string seasonepisode = Common.BuildSeasonEpisodeString(epi.SeasonNum, epi.EpisodeNum);
            string showname = "";
            if (epi.AltShowName != "") { showname = epi.AltShowName; } else { showname = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(epi.CleanedShowName); }
            string findin = Path.Combine(directory, showname, seas);
            try
            {
                string[] files = Directory.GetFiles(findin);
                foreach (string file in files)
                {
                    string medianame = file.Replace(findin, "").Replace("/", "");
                    string trashloc = Path.Combine(Appinfo.HomeDir, "Trash", medianame);
                    log.Write($"File to Delete {medianame} for episode {seasonepisode}", "MediaFileHandler", 4);

                    if (file.ToLower().Contains(seasonepisode.ToLower()))
                    {
                        try
                        {
                            File.Move(file, trashloc);
                            log.Write($"Delete {medianame}, to {trashloc}", "", 4);
                        }
                        catch (Exception e)
                        {
                            log.Write($"Something went wrong moving {medianame} to {trashloc}: {e.Message}", "", 0);
                            using (ActionItems ai = new(Appinfo)) { ai.DbInsert($"Something went wrong moving {medianame} to {trashloc}: {e.Message}"); }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                log.Write($"Error on getting Files for {Path.Combine(directory, showname, seas)}: {e}");
            }

            return success;
        }

        public bool MoveMediaToPlex(string mediainfo, Episode episode)
        {
            bool success = false;
            string fullmediapath = Path.Combine(PlexMediaAcquire, mediainfo);

            FileAttributes atr = File.GetAttributes(fullmediapath);
            bool isdirectory = false;
            List<string> media = new();
            string[] filesindirectory;
            string destdirectory = "";
            switch (episode.MediaType)
            {
                case "TS":
                    destdirectory = PlexMediaTvShows;
                    break;
                case "TSS":
                    destdirectory = PlexMediaTvShowSeries;
                    break;
                case "KTS":
                    destdirectory = PlexMediaKidsTvShows;
                    break;
                default:
                    break;
            }

            atr = File.GetAttributes(fullmediapath);
            if (atr == FileAttributes.Directory) { isdirectory = true; }
            if (!isdirectory) { media.Add(fullmediapath); }
            else
            {
                string[] fullmp = fullmediapath.Split("[");
                if (fullmp.Length == 2)
                {
                    Directory.Move(fullmediapath, fullmp[0]);
                    fullmediapath = fullmp[0];
                }
                filesindirectory = Directory.GetFiles(fullmediapath);
                foreach (string file in filesindirectory)
                {
                    foreach (string ext in Appinfo.MediaExtensions)
                    {
                        log.Write($"Processing {file} with extension {ext}", "", 4);
                        if (file.Contains(ext)) { media.Add(file); break; }
                    }
                }
            }

            if (media.Count == 0) { log.Write($"There was nothing to move {mediainfo}"); }
            string shown;
            if (episode.AltShowName != "") { shown = episode.AltShowName; } else { shown = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(episode.CleanedShowName); }
            string todir = Path.Combine(destdirectory, shown, $"Season {episode.SeasonNum}");
            if (!Directory.Exists(todir)) { Directory.CreateDirectory(todir); }

            foreach (string file in media)
            {
                string fromfile = file.Replace(fullmediapath, "").Replace("/", "");
                string topath = Path.Combine(todir, fromfile);
                try
                {
                    File.Move(file, topath);
                    log.Write($"Moved from {file} to {topath}");
                }
                catch (Exception ex)
                {
                    log.Write($"Error Moving File {file} to {topath} >>> {ex.Message}"); 
                }

            }

            //TODO generalize the Processed or just delete when everything is fully tested
            if (isdirectory) { Directory.Move(fullmediapath, $"{PlexMediaAcquire}/Processed/{mediainfo}"); }

            return success;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
