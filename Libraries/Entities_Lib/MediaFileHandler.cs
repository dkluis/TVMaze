using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using Common_Lib;

using DB_Lib;

using DB_Lib_EF.Entities;

namespace Entities_Lib;

public class MediaFileHandler : IDisposable
{
    private readonly AppInfo         _appInfo;
    private readonly TextFileHandler _log;
    private readonly MariaDb         _mdb;

    public MediaFileHandler(AppInfo appInfo)
    {
        _appInfo = appInfo;
        _mdb     = new MariaDb(appInfo);
        _log     = appInfo.TxtFile;
        GetSetMediaInfo();
    }

    private string PlexMediaAcquire      { get; set; }         = null!;
    private string PlexMediaKidsTvShows  { get; set; }         = null!;
    public  string PlexMediaTvShows      { get; private set; } = null!;
    public  string PlexMediaKimTvShows   { get; private set; } = null!;
    public  string PlexMediaDickTvShows  { get; private set; } = null!;
    private string PlexMediaTvShowSeries { get; set; }         = null!;

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    private void GetSetMediaInfo()
    {
        PlexMediaTvShows      = GetDirectoryViaMediaType("TS");
        PlexMediaTvShowSeries = GetDirectoryViaMediaType("TSS");
        PlexMediaKidsTvShows  = GetDirectoryViaMediaType("KTS");
        PlexMediaAcquire      = GetDirectoryViaMediaType("ACQ").Replace("TVMaze/", "/");
        PlexMediaKimTvShows   = GetDirectoryViaMediaType("KIMTS");
        PlexMediaDickTvShows  = GetDirectoryViaMediaType("DICKTS");
    }

    private string GetMediaDirectory(string mediaType)
    {
        return mediaType switch
               {
                   "TS" => PlexMediaTvShows, "TSS" => PlexMediaTvShowSeries, "KTS" => PlexMediaKidsTvShows, "KIMTS" => PlexMediaKimTvShows, "DICKTS" => PlexMediaDickTvShows, _ => "",
               };
    }

    private string GetDirectoryViaMediaType(string mt)
    {
        var path = "";
        var rdr  = _mdb.ExecQuery($"select `PlexLocation` from `MediaTypes` where `MediaType` = '{mt}'");

        while (rdr.Read())
            path = rdr[0].ToString();
        _mdb.Close();

        return path!.Replace("/Volumes/HD-Data-CA-Server", "/media/psf");
    }

    public bool DeleteEpisodeFiles(Episode epi)
    {
        if (!epi.IsAutoDelete) return true;
        var directory     = GetMediaDirectory(epi.MediaType);
        var seas          = $"Season {epi.SeasonNum}";
        var seasonEpisode = Common.BuildSeasonEpisodeString(epi.SeasonNum, epi.EpisodeNum);
        var showName      = string.IsNullOrEmpty(epi.AltShowName) ? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(epi.CleanedShowName) : epi.AltShowName;
        var findIn        = Path.Combine(directory, showName, seas);

        if (Directory.Exists(findIn))
            try
            {
                var files = Directory.GetFiles(findIn);

                foreach (var file in files)
                {
                    var mediaName = file.Replace(findIn, "").Replace("/", "");
                    var trashLoc  = Path.Combine(_appInfo.HomeDir!, "Trash", mediaName);
                    LogModel.Record(_appInfo.Program, "Media File Handler", $"Evaluating File {mediaName} for episode {seasonEpisode}", 4);

                    if (!file.Contains(seasonEpisode, StringComparison.CurrentCultureIgnoreCase)) continue;

                    try
                    {
                        File.Move(file, trashLoc);
                        LogModel.Record(_appInfo.Program, "Media File Handler", $"Deleted {mediaName}, to {trashLoc}", 2);
                    }
                    catch (Exception e)
                    {
                        LogModel.Record(_appInfo.Program, "Media File Handler", $"Something went wrong moving {mediaName} to {trashLoc}: {e.Message}", 0);
                        ActionItemModel.RecordActionItem(_appInfo.Program, $"Something went wrong moving {mediaName} to {trashLoc}: {e.Message}", _log);
                    }
                }
            }
            catch (Exception e)
            {
                LogModel.Record(_appInfo.Program, "Media File Handler", $"Error on getting Files for {Path.Combine(directory, showName, seas)}: {e.Message}",0);
            }
        else
            LogModel.Record(_appInfo.Program, "Media File Handler", $"Directory {findIn} does not exist", 0);

        return false;
    }

    public bool MoveNonTvMediaToPlex(string mediainfo)
    {
        // Check if it is a Movie via the extension
        var          fullMediaPath = Path.Combine(PlexMediaAcquire, mediainfo);
        List<string> media         = new();
        List<string> mediaFiles    = new();

        try
        {
            if (Directory.Exists(fullMediaPath))
            {
                var files = Directory.GetFiles(fullMediaPath);
                mediaFiles.AddRange(files.Select(file => Path.Combine(PlexMediaAcquire, file)));
            }

            if (File.Exists(fullMediaPath)) mediaFiles.Add(fullMediaPath);
        }
        catch (Exception ex)
        {
            LogModel.Record(_appInfo.Program, "Media File Handler", $"Got an error {ex.Message} trying to access {fullMediaPath}", 0);

            return false;
        }

        if (mediaFiles.Count <= 0)
        {
            LogModel.Record(_appInfo.Program, "Media File Handler", $"Could not find dir and file for {fullMediaPath}", 0);

            return false;
        }

        foreach (var file in mediaFiles)
        foreach (var ext in _appInfo.MediaExtensions)
        {
            LogModel.Record(_appInfo.Program, "Media File Handler", $"Processing {file} with extension {ext}", 4);

            if (!file.Contains(ext)) continue;
            media.Add(file);

            break;
        }

        if (media.Count <= 0)
        {
            LogModel.Record(_appInfo.Program, "Media File Handler", $"Could not find the right Media at {fullMediaPath}", 0);

            return false;
        }

        return false;
    }

    public bool MoveMediaToPlex(string mediainfo, Episode? episode = null, Show? show = null, int season = 99)
    {
        if (episode is null && show is null)
        {
            LogModel.Record(_appInfo.Program, "Media File Handler", $"Episode and Show are set to null, cannot process the move for {mediainfo}", 0);
            ActionItemModel.RecordActionItem(_appInfo.Program, $"Episode and Show are set to null, cannot process the move for {mediainfo}", _log);

            return false;
        }

        string destDirectory;
        string shown;

        if (show != null)
        {
            destDirectory = GetMediaDirectory(show!.MediaType);
            shown         = show.AltShowName != "" ? show.AltShowName : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(show.CleanedShowName);
        } else
        {
            destDirectory = GetMediaDirectory(episode!.MediaType);
            shown         = episode.AltShowName != "" ? episode.AltShowName : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(episode.CleanedShowName);
        }

        var          fullMediaPath = Path.Combine(PlexMediaAcquire, mediainfo);
        var          isDirectory   = false;
        List<string> media         = new();
        var          foundDir      = false;
        var          foundFile     = false;

        try
        {
            if (Directory.Exists(fullMediaPath)) foundDir = true;
            if (File.Exists(fullMediaPath)) foundFile     = true;
        }
        catch (Exception ex)
        {
            LogModel.Record(_appInfo.Program, "Media File Handler", $"Got an error {ex} trying to access {fullMediaPath}", 0);

            return false;
        }

        if (!foundDir && !foundFile)
        {
            LogModel.Record(_appInfo.Program, "Media File Handler", $"Could not find dir and file for {fullMediaPath}", 0);

            return false;
        }

        var atr                                          = File.GetAttributes(fullMediaPath);
        if (atr == FileAttributes.Directory) isDirectory = true;

        if (!isDirectory)
        {
            media.Add(fullMediaPath);
        } else
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
                LogModel.Record(_appInfo.Program, "Media File Handler", $"Processing {file} with extension {ext}", 3);

                if (!file.Contains(ext)) continue;
                media.Add(file);

                break;
            }
        }

        if (media.Count == 0)
        {
            _log.Write($"There was nothing to move {mediainfo}");
            LogModel.Record(_appInfo.Program, "Media File Handler", $"There was nothing to move {mediainfo}", 2);
        }

        // if (string.IsNullOrEmpty(shown) || string.IsNullOrWhiteSpace(shown))
        // {
        //     shown = showName;
        // }

        var toDir = Path.Combine(destDirectory, shown, episode is not null ? $"Season {episode.SeasonNum}" : $"Season {season}");
        if (!Directory.Exists(toDir)) Directory.CreateDirectory(toDir);

        foreach (var file in media)
        {
            var fromFile = !isDirectory ? file.Replace(PlexMediaAcquire, "").Replace("/", "") : file.Replace(fullMediaPath, "").Replace("/", "");
            var toPath   = Path.Combine(toDir, fromFile);

            try
            {
                File.Move(file, toPath);
                LogModel.Record(_appInfo.Program, "Media File Handler", $"Moved To: {toPath}", 2);
                _log.Write($"Moved To: {toPath}");
            }
            catch (Exception ex)
            {
                LogModel.Record(_appInfo.Program, "Media File Handler", $"Error Moving File {file} to {toPath} >>> {ex.Message}", 0);
            }
        }

        if (isDirectory)
        {
            Directory.Move(fullMediaPath, $"{PlexMediaAcquire}/Processed/{mediainfo}");
            LogModel.Record(_appInfo.Program, "Media File Handler", $"Moved {fullMediaPath} to {PlexMediaAcquire}/Processed/{mediainfo}", 3);
        }

        return false;
    }
}
