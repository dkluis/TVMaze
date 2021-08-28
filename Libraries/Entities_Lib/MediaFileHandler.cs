using System;
using System.Collections.Generic;
using System.IO;
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

        public bool DeleteTvShows;
        public bool DeteteTvShowsSeries;
        public bool DeleteKidsTvShows;

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
            DeleteTvShows = GetDeleteViaMediaType("TS");
            PlexMediaTvShowSeries = GetDirectoryViaMediaType("TSS");
            DeleteTvShows = GetDeleteViaMediaType("TSS");
            PlexMediaKidsTvShows = GetDirectoryViaMediaType("KTS");
            DeleteTvShows = GetDeleteViaMediaType("KTS");

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

        public bool GetDeleteViaMediaType(string mt)
        {
            bool delete = false;
            MySqlDataReader rdr = Mdb.ExecQuery($"select `AutoDelete` from `MediaTypes` where `MediaType` = '{mt}'");
            if (rdr is not null)
            {
                while (rdr.Read())
                {
                    if (rdr[0].ToString() == "Yes") { delete = true; } 
                }
            }
            Mdb.Close();
            return delete;
        }

        public List<string> GetMediaByShow(string mediatype, string show, int season, int episode, bool delete = false)
        {
            List<string> FilesInDirectory = new();
            string directory = "";
            switch (mediatype)
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
            string seas = $"Season {season}";
            string seasonepisode = Common.BuildSeasonEpisodeString(season, episode);
            string findin = Path.Combine(directory, show, seas);
            string[] files = Directory.GetFiles(findin);

            foreach (string file in files)
            {
                if (file.Contains(seasonepisode))
                {
                    FilesInDirectory.Add(file);
                    if (delete)
                    {
                        string f = file.Replace(findin, "").Replace("/", "");
                        string trashloc = Path.Combine(Appinfo.HomeDir, "Trash", f);
                        try
                        {
                            File.Move(file, trashloc);
                            log.Write($"Delete {f}, to {trashloc}", "", 4);
                        }
                        catch (Exception e)
                        {
                            log.Write($"Something went wrong moving to trash {f}, {trashloc} {e.Message}", "", 0);
                            using (ActionItems ai = new(Appinfo)) { ai.DbInsert($"Something went wrong moving to trash {f}, {trashloc} {e.Message}"); }
                        }
                    }
                }
            }

            return FilesInDirectory;
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
            if (epi.AltShowName != "") { showname = epi.AltShowName; } else { showname = epi.ShowName; }
            string findin = Path.Combine(directory, showname, seas);
            string[] files = Directory.GetFiles(findin);

            foreach (string file in files)
            { 
                log.Write($"File to Delete {file}", "MediaFileHandler", 4);
            }

            return success;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
