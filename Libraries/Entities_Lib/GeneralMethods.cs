using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Common_Lib;
using DB_Lib_EF.Entities;
using DB_Lib_EF.Models.MariaDB;

namespace Entities_Lib;

public static class GeneralMethods
{
    public static FindShowEpisodeResult FindShowEpisodeInfo(string program, string showEpisode)
    {
        var result = new FindShowEpisodeResult();

        if (string.IsNullOrEmpty(showEpisode))
        {
            LogModel.Record(program, "FindShowEpisode", "Input was null or empty", 20);
            result.Message = "Input was null or empty";

            return result;
        }

        var seInfo = Regex.Split(showEpisode, "S[0-9]+E[0-9]+.", RegexOptions.IgnoreCase);
        var seSeas = Regex.Split(showEpisode, "S[0-9]+.",        RegexOptions.IgnoreCase);

        if (seInfo.Length == 2)
        {
            result.ShowName      = seInfo[0].Replace(".", " ").Trim();
            result.EpisodeString = showEpisode.Replace(seInfo[0], "").Replace(seInfo[1] == "" ? "   " : seInfo[1], "").Replace(".", "").Trim();
        } else if (seSeas.Length == 2)
        {
            result.ShowName = seSeas[0].Replace(".", " ").Trim();
            ;
            result.EpisodeString = showEpisode.Replace(seSeas[0], "").Replace(seSeas[1] == "" ? "   " : seSeas[1], "").Replace(".", "").Trim();
            result.IsSeason      = true;
        } else
        {
            result.Message = $"Show Title: {showEpisode} regex result had {seInfo.Length} S99e99 pieces and {seSeas.Length} S99 pieces";
            LogModel.Record(program, "FindShowEpisode", $"Show Title: {showEpisode} regex result had {seInfo.Length} S99e99 pieces and {seSeas.Length} S99 pieces", 20);

            return result;
        }

        var foundViaShowName = FindShowViaName(program, result.ShowName);

        var searchShowsViaNames = new SearchShowsViaNames();
        var foundShowIds        = searchShowsViaNames.Find(new AppInfo("TVMaze", program, "DbAlternate"), result.ShowName);

        if (foundShowIds.Count != 1)
        {
            result.Message = $"Show Title: {showEpisode} found {foundShowIds.Count} records in the shows table";
            LogModel.Record(program, "FindShowEpisode", $"Show Title: {showEpisode} found {foundShowIds.Count} records in the shows table", 20);

            return result;
        }

        result.TvmShowId = foundShowIds[0];

        var db      = new TvMaze();
        var episode = db.Episodes.SingleOrDefault(e => e.TvmShowId == result.TvmShowId && e.SeasonEpisode == result.EpisodeString.ToLower());

        if (episode != null)
        {
            result.TvmShowId    = episode.TvmShowId;
            result.TvmEpisodeId = episode.TvmEpisodeId;
            result.Found        = true;
            result.IsWatched    = episode.PlexStatus == "Watched";

            return result;
        }
        if (result.IsSeason)
        {
            var seStr = result.EpisodeString + "e01";
            var epi   = db.Episodes.SingleOrDefault(e => e.TvmShowId == result.TvmShowId && e.SeasonEpisode == seStr.ToLower());

            if (epi != null)
            {
                //result.TvmShowId    = epi.TvmShowId;
                result.TvmEpisodeId = epi.TvmEpisodeId;
                result.Found        = true;
                result.IsWatched    = epi.PlexStatus == "Watched";
                result.Message      = "Found via Episode 1 of the season";

                return result;
            }
        }

        LogModel.Record(program, "FindShowEpisode", $"No episode found for Show Title: {showEpisode}", 20);
        result.Message = "No episode found";

        return result;
    }

    public static FindShowViaNameResult FindShowViaName(string program, string showName)
    {
        var result = new FindShowViaNameResult();

        try
        {
            var found = new List<int>();
            showName = showName.Replace(" ", " ");
            var altShowName     = showName;
            var cleanedShowName = Common.RemoveSuffixFromShowName(Common.RemoveSpecialCharsInShowName(showName)).Replace("ʻ", "");

            if (altShowName == "") altShowName = showName;

            var       tvmStatusList = new List<string> {"Ended", "Skipping"};
            var       dateStr       = "1900-01-01";
            var       premDate      = DateOnly.Parse(dateStr);
            using var db            = new TvMaze();

            var shows             = db.Shows.Where(s => (s.ShowName == showName || s.CleanedShowName == cleanedShowName || s.AltShowname == altShowName) && !tvmStatusList.Contains(s.TvmStatus) && s.PremiereDate != premDate).ToList();
            var inclSkipping      = shows.Count;
            var exclSkipping      = 0;
            var alternateShowName = 0;

            if (shows.Count == 1)
            {
                foreach (var show in shows)
                {
                    result.Found    = true;
                    result.FoundVia = "Including Skipping";
                    result.Show     = show;
                }
            } else
            {
                shows        = db.Shows.Where(s => (s.ShowName == showName || s.CleanedShowName == cleanedShowName || s.AltShowname == altShowName) && s.ShowStatus != "Skipping").ToList();
                exclSkipping = shows.Count;

                if (shows.Count == 1)
                {
                    foreach (var show in shows)
                    {
                        result.Found    = true;
                        result.FoundVia = "Excluding Skipping";
                        result.Show     = show;
                    }
                } else
                {
                    var splitShowName                         = altShowName.Split(" (");
                    if (splitShowName.Length > 0) altShowName = splitShowName[0];
                    shows             = db.Shows.Where(s => (s.ShowName == showName || s.CleanedShowName == cleanedShowName || s.AltShowname == altShowName) && s.ShowStatus != "Skipping").ToList();
                    alternateShowName = shows.Count;

                    if (shows.Count == 1)
                    {
                        foreach (var show in shows)
                        {
                            result.Found    = true;
                            result.FoundVia = "Alternate ShowName";
                            result.Show     = show;
                        }
                    } else
                    {
                        result.Found   = false;
                        result.Message = $"Found Including Skipping {inclSkipping} records, Found Excluding Skipping {exclSkipping} records, Found Alternate ShowName {alternateShowName} records";
                    }
                }
            }

            return result;
        }
        catch (Exception e)
        {
            LogModel.Record(program, "Show Entity", $"Error during Find {e.Message} :::{e.InnerException}", 20);

            return result;
        }
    }

    public class FindShowEpisodeResult
    {
        public bool                              Found         { get; set; }
        public string                            Message       { get; set; } = string.Empty;
        public string                            ShowName      { get; set; } = string.Empty;
        public string                            EpisodeString { get; set; } = string.Empty;
        public bool                              IsSeason      { get; set; }
        public int                               TvmShowId     { get; set; }
        public int                               TvmEpisodeId  { get; set; }
        public bool                              IsWatched     { get; set; }
        public DB_Lib_EF.Models.MariaDB.Show?    Show          { get; set; }
        public DB_Lib_EF.Models.MariaDB.Episode? Episode       { get; set; }
    }

    public class FindShowViaNameResult
    {
        public bool                           Found    { get; set; }
        public string                         Message  { get; set; } = string.Empty;
        public DB_Lib_EF.Models.MariaDB.Show? Show     { get; set; }
        public string                         FoundVia { get; set; } = string.Empty;
    }
}
