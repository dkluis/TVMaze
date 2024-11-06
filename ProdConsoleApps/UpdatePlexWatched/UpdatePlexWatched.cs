using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Common_Lib;
using DB_Lib;
using DB_Lib_EF.Entities;
using Entities_Lib;
using Web_Lib;
using Episode = Entities_Lib.Episode;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace UpdatePlexWatched;

/// <summary>
///     1. Reads the Plex Sqlite DB and get all Episodes that are marked Watched in the last day
///     2. Figures out from the Plex ShowName, season and episode numbers what TvmShowId and TvmEpisodeId it is in Tvmaze
///     Local
///     3. Updates Tvmaze Local and Tvmaze Web with the Watched status and the date
///     a. Adds a record in Tvmaze Local to track if an episode is already updated
///     4. Delete media from Plex if Auto Delete is set for that Show
/// </summary>
internal static class UpdatePlexWatched
{
    private static void Main()
    {
        const string thisProgram = "Update Plex Watched";
        AppInfo      appInfo     = new("TVMaze", thisProgram, "DbAlternate");
        LogModel.Start(thisProgram);

        //var watchedEpisodes = PlexSqlLite.PlexWatched(appInfo);
        var watchedEpisodes = new List<PlexWatchedInfo>();

        try
        {
            const string filePath = "/users/dick/TVMaze/Inputs/WatchedEpisodes.log";
            var toFilePath = $"/users/dick/TVMaze/Inputs/WatchedEpisodes{DateTime.Now.ToString("-yyyy-M-d-hh-mm-ss")}.log";
            var lines      = File.ReadAllLines(filePath);

            foreach (var line in lines)
            {
                var watchedEpi = JsonSerializer.Deserialize<ShowData>(line);

                if (watchedEpi == null)
                {
                    LogModel.Record(thisProgram, "Main", $"Error Occurred with Json Deserialize of a line: {line}", 20);

                    continue;
                }

                var cleanedShowName = Common.RemoveSpecialCharsInShowName(watchedEpi.ShowName!).Replace("ʻ", "");
                var watchedInfo     = new PlexWatchedInfo();
                watchedInfo.ShowName          = watchedEpi.ShowName!;
                watchedInfo.SeasonEpisode     = watchedEpi.SeasonEpisode!;
                watchedInfo.CleanedShowName   = cleanedShowName.Replace("ʻ", "");
                watchedInfo.UpdateDate        = watchedEpi.UpdateDate!;
                watchedInfo.WatchedDate       = watchedEpi.WatchedDate;
                watchedInfo.TvmEpisodeId      = watchedEpi.TvmEpisodeId;
                watchedInfo.TvmShowId         = watchedEpi.TvmShowId;
                watchedInfo.ProcessedToTvmaze = watchedEpi.ProcessedToTvmaze;
                var match = Regex.Match(watchedEpi.SeasonEpisode!, @"s(\d+)e(\d+)", RegexOptions.IgnoreCase);
                watchedInfo.Season  = int.Parse(match.Groups[1].Value);
                watchedInfo.Episode = int.Parse(match.Groups[2].Value);
                watchedEpisodes.Add(watchedInfo);
            }

            // Delete the file
            File.Move(filePath, toFilePath);
        }
        catch (Exception e)
        {
            LogModel.Record(thisProgram, "Main", $"Error Occured Reading WatchedEpisodes.log {e.Message} ::: {e.InnerException}", 20);

            throw;
        }

        if (watchedEpisodes.Count > 0)
            foreach (var pwi in watchedEpisodes)
            {
                SearchShowsViaNames searchShowViaNames = new();
                var                 foundInDb          = searchShowViaNames.Find(appInfo, pwi.ShowName, pwi.CleanedShowName);

                switch (foundInDb.Count)
                {
                    case 1:
                    {
                        pwi.TvmShowId = foundInDb[0];
                        using EpisodeSearch es = new();
                        pwi.TvmEpisodeId = es.Find(appInfo, pwi.TvmShowId, pwi.SeasonEpisode);

                        if (pwi.TvmEpisodeId == 0)
                        {
                            LogModel.Record(thisProgram, "Main", $"Found Show: {pwi.ShowName} but not the Episode.  No update of TVMaze and No Delete of File");
                            ActionItemModel.RecordActionItem(thisProgram, $"Found Show: {pwi.ShowName} but not the Episode.  No update of TVMaze and No Delete of File");

                            continue;
                        }

                        LogModel.Record(thisProgram, "Main", $"Found Show: {pwi.ShowName}", 5);

                        break;
                    }

                    case > 1:
                    {
                        foreach (var showId in foundInDb)
                        {
                            ActionItemModel.RecordActionItem(thisProgram, $"Multiple ShowIds found for {pwi.ShowName} is: {showId}");
                            LogModel.Record(thisProgram, "Main", $"Found multiple Shows named: {pwi.ShowName}", 5);
                        }

                        break;
                    }

                    default:
                    {
                        ActionItemModel.RecordActionItem(thisProgram, $"Did not find any ShowIds for {pwi.ShowName}");
                        LogModel.Record(thisProgram, "Main", $"NotFound Show: {pwi.ShowName}", 5);

                        break;
                    }
                }

                LogModel.Record(thisProgram, "Main", $"ShowId found for {pwi.ShowName}: ShowId: {pwi.TvmShowId}, EpisodeId: {pwi.TvmEpisodeId}");

                if (!pwi.DbInsert(appInfo))
                {
                    LogModel.Record(thisProgram, "Main", "Show and Episode already updated");

                    continue;
                }

                if (pwi.TvmEpisodeId == 0)
                {
                    LogModel.Record(thisProgram, "Main", $"TvmEpisodeId is 0 for {pwi.ShowName} - {pwi.SeasonEpisode}", 4);
                    ActionItemModel.RecordActionItem(thisProgram, $"TvmEpisodeId is 0 for {pwi.ShowName} - {pwi.SeasonEpisode}");

                    continue;
                }

                using Episode epi = new(appInfo);
                epi.FillViaTvmaze(pwi.TvmEpisodeId);
                using WebApi wa = new(appInfo);
                wa.PutEpisodeToWatched(epi.TvmEpisodeId, pwi.WatchedDate);
                epi.PlexDate   = pwi.WatchedDate;
                epi.PlexStatus = "Watched";
                epi.DbUpdate();
                LogModel.Record(thisProgram, "Main", $"Update Episode Record for Show {pwi.ShowName} {epi.TvmEpisodeId}, {epi.PlexDate}, {epi.PlexStatus}");
                pwi.ProcessedToTvmaze = true;
                pwi.DbUpdate(appInfo);

                if (epi.IsAutoDelete)
                {
                    LogModel.Record(thisProgram, "Main", $"Deleting this episode {pwi.ShowName} - {pwi.SeasonEpisode} file");
                    using MediaFileHandler mfh = new(appInfo);
                    _ = mfh.DeleteEpisodeFiles(epi);
                }

                pwi.Reset();
            }
        else LogModel.Record(thisProgram, "Main", "No Watched Episodes Found");

        LogModel.Stop(thisProgram);
    }

    public class ShowData
    {
        public string? ShowName          { get; set; }
        public string? SeasonEpisode     { get; set; }
        public string? CleanedShowName   { get; set; }
        public string? UpdateDate        { get; set; }
        public string? WatchedDate       { get; set; }
        public int     TvmEpisodeId      { get; set; }
        public int     TvmShowId         { get; set; }
        public bool    ProcessedToTvmaze { get; set; }
        public int     Season            { get; set; }
        public int     Episode           { get; set; }
    }
}
