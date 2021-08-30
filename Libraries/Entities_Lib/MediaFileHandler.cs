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

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
