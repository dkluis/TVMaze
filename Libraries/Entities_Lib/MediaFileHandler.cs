using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Common_Lib;
using DB_Lib;

namespace Entities_Lib;

public class MediaFileHandler : IDisposable
{
    private readonly AppInfo _appInfo;
    private readonly TextFileHandler _log;
    private readonly MariaDb _mdb;

    public MediaFileHandler(AppInfo appInfo)
    {
        _appInfo = appInfo;
        _mdb = new MariaDb(appInfo);
        _log = appInfo.TxtFile;
        GetSetMediaInfo();
    }

    private string PlexMediaAcquire { get; set; } = null!;
    private string PlexMediaKidsTvShows { get; set; } = null!;
    public string PlexMediaTvShows { get; private set; } = null!;
    public string PlexMediaKimTvShows { get; private set; } = null!;
    public string PlexMediaDickTvShows { get; set; } = null!;
    private string PlexMediaTvShowSeries { get; set; } = null!;

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    private void GetSetMediaInfo()
    {
        PlexMediaTvShows = GetDirectoryViaMediaType("TS");
        PlexMediaTvShowSeries = GetDirectoryViaMediaType("TSS");
        PlexMediaKidsTvShows = GetDirectoryViaMediaType("KTS");
        PlexMediaAcquire = GetDirectoryViaMediaType("ACQ");
        PlexMediaKimTvShows = GetDirectoryViaMediaType("KIMTS");
        PlexMediaDickTvShows = GetDirectoryViaMediaType("DICKTS");
    }

    private string GetMediaDirectory(string mediaType)
    {
        return mediaType switch
        {
            "TS" => PlexMediaTvShows,
            "TSS" => PlexMediaTvShowSeries,
            "KTS" => PlexMediaKidsTvShows,
            "KIMTS" => PlexMediaKimTvShows,
            "DICKTS" => PlexMediaDickTvShows,
            _ => ""
        };
    }

    private string GetDirectoryViaMediaType(string mt)
    {
        var path = "";
        var rdr = _mdb.ExecQuery($"select `PlexLocation` from `MediaTypes` where `MediaType` = '{mt}'");
        while (rdr.Read())
            path = rdr[0].ToString();
        _mdb.Close();
        return path!;
    }

    public bool DeleteEpisodeFiles(Episode epi)
    {
        if (!epi.IsAutoDelete) return true;
        var directory = GetMediaDirectory(epi.MediaType);
        var seas = $"Season {epi.SeasonNum}";
        var seasonEpisode = Common.BuildSeasonEpisodeString(epi.SeasonNum, epi.EpisodeNum);
        var showName = epi.AltShowName != ""
            ? epi.AltShowName
            : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(epi.CleanedShowName);
        var findIn = Path.Combine(directory, showName, seas);
        if (Directory.Exists(findIn))
            try
            {
                var files = Directory.GetFiles(findIn);
                foreach (var file in files)
                {
                    var mediaName = file.Replace(findIn, "").Replace("/", "");
                    var trashLoc = Path.Combine(_appInfo.HomeDir, "Trash", mediaName);
                    _log.Write($"File to Delete {mediaName} for episode {seasonEpisode}", "MediaFileHandler", 4);
                    if (!file.ToLower().Contains(seasonEpisode.ToLower())) continue;
                    try
                    {
                        File.Move(file, trashLoc);
                        _log.Write($"Delete {mediaName}, to {trashLoc}", "", 4);
                    }
                    catch (Exception e)
                    {
                        _log.Write($"Something went wrong moving {mediaName} to {trashLoc}: {e.Message}", "", 0);
                        using ActionItems ai = new(_appInfo);
                        ai.DbInsert($"Something went wrong moving {mediaName} to {trashLoc}: {e.Message}");
                    }
                }
            }
            catch (Exception e)
            {
                _log.Write($"Error on getting Files for {Path.Combine(directory, showName, seas)}: {e}");
            }
        else
            _log.Write($"Directory {findIn} does not exist");

        return false;
    }

    public bool MoveNonTvMediaToPlex(string mediainfo)
    {
        // Check if it is a Movie via the extension
        var fullMediaPath = Path.Combine(PlexMediaAcquire, mediainfo);
        List<string> media = new();
        List<string> mediaFiles = new();
        try
        {
            if (Directory.Exists(fullMediaPath))
            {
                var files = Directory.GetFiles(fullMediaPath);
                foreach (var file in files) mediaFiles.Add(Path.Combine(PlexMediaAcquire, file));
            }

            if (File.Exists(fullMediaPath)) mediaFiles.Add(fullMediaPath);
        }
        catch (Exception ex)
        {
            _log.Write($"Got an error {ex} trying to access {fullMediaPath}");
            return false;
        }

        if (mediaFiles.Count <= 0)
        {
            _log.Write($"Could not find dir and file for {fullMediaPath}", "", 0);
            return false;
        }

        foreach (var file in mediaFiles)
        foreach (var ext in _appInfo.MediaExtensions)
        {
            _log.Write($"Processing {file} with extension {ext}", "", 4);
            if (!file.Contains(ext)) continue;
            media.Add(file);
            break;
        }

        if (media.Count <= 0)
        {
            _log.Write($"Could not find the right Media {fullMediaPath}", "", 0);
            return false;
        }


        return false;
    }

    public bool MoveMediaToPlex(string mediainfo, Episode? episode = null, Show? show = null, int season = 99)
    {
        if (episode is null && show is null)
        {
            _log.Write($"Episode and Show are set to null, cannot process the move for {mediainfo}", "", 0);
            using ActionItems ai = new(_appInfo);
            ai.DbInsert($"Episode and Show are set to null, cannot process the move for {mediainfo}");
            return false;
        }

        string destDirectory;
        string shown;
        if (episode is not null)
        {
            destDirectory = GetMediaDirectory(episode.MediaType);
            shown = episode.AltShowName != ""
                ? episode.AltShowName
                : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(episode.CleanedShowName);
        }
        else
        {
            destDirectory = GetMediaDirectory(show!.MediaType);

            shown = show.AltShowName != ""
                ? show.AltShowName
                : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(show.CleanedShowName);
        }

        var fullMediaPath = Path.Combine(PlexMediaAcquire, mediainfo);
        var isDirectory = false;
        List<string> media = new();
        var foundDir = false;
        var foundFile = false;
        try
        {
            if (Directory.Exists(fullMediaPath)) foundDir = true;
            if (File.Exists(fullMediaPath)) foundFile = true;
        }
        catch (Exception ex)
        {
            _log.Write($"Got an error {ex} trying to access {fullMediaPath}");
            return false;
        }

        if (!foundDir && !foundFile)
        {
            _log.Write($"Could not find dir and file for {fullMediaPath}", "", 0);
            return false;
        }

        var atr = File.GetAttributes(fullMediaPath);
        if (atr == FileAttributes.Directory) isDirectory = true;
        if (!isDirectory)
        {
            media.Add(fullMediaPath);
        }
        else
        {
            var medPatLen = fullMediaPath.Split("[");
            if (medPatLen.Length == 2)
            {
                Directory.Move(fullMediaPath, medPatLen[0]);
                fullMediaPath = medPatLen[0];
            }

            var filesInDirectory = Directory.GetFiles(fullMediaPath);
            foreach (var file in filesInDirectory)
            foreach (var ext in _appInfo.MediaExtensions)
            {
                _log.Write($"Processing {file} with extension {ext}", "", 4);
                if (!file.Contains(ext)) continue;
                media.Add(file);
                break;
            }
        }

        if (media.Count == 0) _log.Write($"There was nothing to move {mediainfo}");
        var toDir = Path.Combine(destDirectory, shown, episode is not null
            ? $"Season {episode.SeasonNum}"
            : $"Season {season}");
        if (!Directory.Exists(toDir)) Directory.CreateDirectory(toDir);
        foreach (var file in media)
        {
            var fromFile = !isDirectory
                ? file.Replace(PlexMediaAcquire, "").Replace("/", "")
                : file.Replace(fullMediaPath, "").Replace("/", "");
            var toPath = Path.Combine(toDir, fromFile);
            try
            {
                File.Move(file, toPath);
                _log.Write($"Moved To: {toPath}");
            }
            catch (Exception ex)
            {
                _log.Write($"Error Moving File {file} to {toPath} >>> {ex.Message}");
            }
        }

        if (isDirectory) Directory.Move(fullMediaPath, $"{PlexMediaAcquire}/Processed/{mediainfo}");
        return false;
    }
}