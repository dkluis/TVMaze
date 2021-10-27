using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Common_Lib;
using DB_Lib;

namespace Entities_Lib
{
    public class MediaFileHandler : IDisposable
    {
        private readonly AppInfo _appinfo;
        private readonly TextFileHandler _log;

        private readonly MariaDb _mdb;
        public List<string> MoviesinSeries;
        public string PlexMediaAcquire;
        public string PlexMediaKidsTvShows;
        public string PlexMediaTvShows;
        public string PlexMediaTvShowSeries;
        public List<string> TvShowsInKids;

        public List<string> TvShowsInSeries;

        public MediaFileHandler(AppInfo appinfo)
        {
            _appinfo = appinfo;
            _mdb = new MariaDb(appinfo);
            _log = appinfo.TxtFile;

            GetSetMediaInfo();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
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
            var path = "";
            var rdr = _mdb.ExecQuery($"select `PlexLocation` from `MediaTypes` where `MediaType` = '{mt}'");
            if (rdr is not null)
                while (rdr.Read())
                    path = rdr[0].ToString();
            _mdb.Close();
            return path;
        }

        public bool DeleteEpisodeFiles(Episode epi)
        {
            if (!epi.IsAutoDelete) return true;
            var success = false;

            var directory = "";
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
            }

            var seas = $"Season {epi.SeasonNum}";
            var seasonepisode = Common.BuildSeasonEpisodeString(epi.SeasonNum, epi.EpisodeNum);
            string showname;
            if (epi.AltShowName != "")
                showname = epi.AltShowName;
            else
                showname = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(epi.CleanedShowName);
            var findin = Path.Combine(directory, showname, seas);
            if (Directory.Exists(findin))
                try
                {
                    var files = Directory.GetFiles(findin);
                    foreach (var file in files)
                    {
                        var medianame = file.Replace(findin, "").Replace("/", "");
                        var trashloc = Path.Combine(_appinfo.HomeDir, "Trash", medianame);
                        _log.Write($"File to Delete {medianame} for episode {seasonepisode}", "MediaFileHandler", 4);

                        if (file.ToLower().Contains(seasonepisode.ToLower()))
                            try
                            {
                                File.Move(file, trashloc);
                                _log.Write($"Delete {medianame}, to {trashloc}", "", 4);
                            }
                            catch (Exception e)
                            {
                                _log.Write($"Something went wrong moving {medianame} to {trashloc}: {e.Message}", "", 0);
                                using (ActionItems ai = new(_appinfo))
                                {
                                    ai.DbInsert($"Something went wrong moving {medianame} to {trashloc}: {e.Message}");
                                }
                            }
                    }
                }
                catch (Exception e)
                {
                    _log.Write($"Error on getting Files for {Path.Combine(directory, showname, seas)}: {e}");
                }
            else
                _log.Write($"Directory {findin} does not exist");

            return success;
        }

        public bool MoveNonTvMediaToPlex(string mediainfo)
        {
            var success = false;
            //TODO handle non TVShow media

            return success;
        }

        public bool MoveMediaToPlex(string mediainfo, Episode episode = null, Show show = null, int season = 99)
        {
            if (episode is null && show is null)
            {
                _log.Write($"Episode and Show are set to null, cannot process the move for {mediainfo}", "", 0);
                using (ActionItems ai = new(_appinfo))
                {
                    ai.DbInsert($"Episode and Show are set to null, cannot process the move for {mediainfo}");
                }

                return false;
            }

            var destdirectory = "";
            var shown = "";
            if (episode is not null)
            {
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
                }

                if (episode.AltShowName != "")
                    shown = episode.AltShowName;
                else
                    shown = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(episode.CleanedShowName);
            }
            else
            {
                switch (show.MediaType)
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
                }

                if (show.AltShowName != "")
                    shown = show.AltShowName;
                else
                    shown = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(show.CleanedShowName);
            }

            var success = false;
            var fullmediapath = Path.Combine(PlexMediaAcquire, mediainfo);
            var isdirectory = false;
            List<string> media = new();
            string[] filesindirectory;
            FileAttributes atr = new();
            var founddir = false;
            var foundfile = false;

            try
            {
                if (Directory.Exists(fullmediapath)) founddir = true;
                if (File.Exists(fullmediapath)) foundfile = true;
            }
            catch (Exception ex)
            {
                _log.Write($"Got an error {ex} trying to access {fullmediapath}");
                return success;
            }

            if (!founddir && !foundfile)
            {
                _log.Write($"Could not find dir {founddir} or file {foundfile} for {fullmediapath}", "", 0);
                return success;
            }

            atr = File.GetAttributes(fullmediapath);
            if (atr == FileAttributes.Directory) isdirectory = true;
            if (!isdirectory)
            {
                media.Add(fullmediapath);
            }
            else
            {
                var fullmp = fullmediapath.Split("[");
                if (fullmp.Length == 2)
                {
                    Directory.Move(fullmediapath, fullmp[0]);
                    fullmediapath = fullmp[0];
                }

                filesindirectory = Directory.GetFiles(fullmediapath);
                foreach (var file in filesindirectory)
                foreach (var ext in _appinfo.MediaExtensions)
                {
                    _log.Write($"Processing {file} with extension {ext}", "", 4);
                    if (file.Contains(ext))
                    {
                        media.Add(file);
                        break;
                    }
                }
            }

            if (media.Count == 0) _log.Write($"There was nothing to move {mediainfo}");
            var todir = "";
            if (episode is not null)
                todir = Path.Combine(destdirectory, shown, $"Season {episode.SeasonNum}");
            else
                todir = Path.Combine(destdirectory, shown, $"Season {season}");
            if (!Directory.Exists(todir)) Directory.CreateDirectory(todir);

            foreach (var file in media)
            {
                var fromfile = "";
                if (!isdirectory)
                    fromfile = file.Replace(PlexMediaAcquire, "").Replace("/", "");
                else
                    fromfile = file.Replace(fullmediapath, "").Replace("/", "");
                var topath = Path.Combine(todir, fromfile);
                try
                {
                    File.Move(file, topath);
                    _log.Write($"Moved To: {topath}");
                }
                catch (Exception ex)
                {
                    _log.Write($"Error Moving File {file} to {topath} >>> {ex.Message}");
                }
            }

            if (isdirectory) Directory.Move(fullmediapath, $"{PlexMediaAcquire}/Processed/{mediainfo}");
            return success;
        }
    }
}