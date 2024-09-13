using System;
using System.Collections.Generic;
using System.Linq;
using Common_Lib;
using DB_Lib;
using DB_Lib_EF.Entities;
using Web_Lib;

namespace Entities_Lib;

public class ShowAndEpisodes : IDisposable
{
    private readonly AppInfo       _appInfo;
    private readonly Show          _show;
    private          List<Episode> _epsByShow;
    private          List<int>     _epsByShowInDb;
    private          List<int>     _epsByShowOnTvmaze;

    public ShowAndEpisodes(AppInfo appInfo)
    {
        _appInfo           = appInfo;
        _show              = new Show(_appInfo);
        _epsByShowOnTvmaze = new List<int>();
        _epsByShow         = new List<Episode>();
        _epsByShowInDb     = new List<int>();
    }

    public void Dispose() { GC.SuppressFinalize(this); }

    public void Refresh(int showId, bool ignore = false)
    {
        _show.FillViaTvmaze(showId);
        _epsByShowOnTvmaze = new List<int>();

        if (_show is {IsDbFilled: true, IsFollowed: true,} && _show.Finder != "Skip" && _show.UpdateDate != "2200-01-01")
            _show.TvmStatus = "Following";

        if (_show is {IsDbFilled: true, Finder: "Skip",} || _show.UpdateDate == "2200-01-01") _show.TvmStatus = "Skipping";

        if (_show.IsDbFilled) _show.DbUpdate();
        else _show.DbInsert();

        using EpisodesByShow epsByShow = new();
        var                  tvmApi    = new WebApi(_appInfo);
        _epsByShow = epsByShow.Find(_appInfo, showId);

        foreach (var episode in _epsByShow)
        {
            if (_show.TvmStatus == "Skipping" && episode.PlexStatus != "Watched" && episode.PlexStatus != "Skipped")
            {
                episode.PlexStatus = "Skipped";
                tvmApi.PutEpisodeToSkipped(episode.TvmEpisodeId);

                continue;
            }

            if (episode.PlexStatus == "Skipped") continue;

            _epsByShowOnTvmaze.Add(episode.TvmEpisodeId);

            if (episode.IsDbFilled) episode.DbUpdate();
            else episode.DbInsert();
        }

        _show.Reset();

        // Check to see if the number of episode on Tvmaze equal the number in Tvmaze Local
        _epsByShowInDb = new List<int>();

        using (MariaDb mDb = new(_appInfo))
        {
            var rdr = mDb.ExecQuery($"select TvmEpisodeId from Episodes where TvmShowId = {showId}");
            while (rdr.Read()) _epsByShowInDb.Add(int.Parse(rdr[0].ToString()!));
        }

        if (_epsByShowInDb.Count > _epsByShowOnTvmaze.Count)
        {
            LogModel.Record(_appInfo.Program, "Show And Episodes", $"More Episodes in DB {_epsByShowInDb.Count} Episodes on TVMaze {_epsByShowOnTvmaze.Count}", 5);
            _epsByShowInDb.Sort();
            _epsByShowOnTvmaze.Sort();
            var           toDeleteEpisodes = _epsByShowInDb.Except(_epsByShowOnTvmaze);
            using Episode episodeToDelete  = new(_appInfo);

            foreach (var episode in toDeleteEpisodes)
            {
                episodeToDelete.FillViaDb(episode);
                episodeToDelete.TvmEpisodeId = episode;
                if (episodeToDelete.IsDbFilled) episodeToDelete.DbDelete();
            }
        } else if (_epsByShowInDb.Count < _epsByShowOnTvmaze.Count)
        {
            LogModel.Record(_appInfo.Program, "Show And Episodes", $"Less Episodes in DB {_epsByShowInDb.Count} Episodes on TVMaze {_epsByShowOnTvmaze.Count}", 5);
        }
    }
}
