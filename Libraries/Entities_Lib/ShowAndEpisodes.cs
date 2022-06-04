using System;
using System.Collections.Generic;
using System.Linq;
using Common_Lib;
using DB_Lib;

namespace Entities_Lib
{
    public class ShowAndEpisodes : IDisposable
    {
        private readonly AppInfo _appInfo;
        private readonly Show _show;
        private readonly TextFileHandler _log;
        private List<Episode> _epsByShow;
        private List<int> _epsByShowInDb;
        private List<int> _epsByShowOnTvmaze;

        public ShowAndEpisodes(AppInfo appInfo)
        {
            _appInfo = appInfo;
            _show = new Show(_appInfo);
            _log = _appInfo.TxtFile;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public void Refresh(int showId, bool ignore = false)
        {
            _show.FillViaTvmaze(showId);
            _epsByShowOnTvmaze = new List<int>();
            if (_show.IsDbFilled && _show.IsFollowed) _show.TvmStatus = "Following";
            _show.DbUpdate();
            _show.Reset();
            using EpisodesByShow epsByShow = new();
            _epsByShow = epsByShow.Find(_appInfo, showId);
            foreach (var episode in _epsByShow)
            {
                _epsByShowOnTvmaze.Add(episode.TvmEpisodeId);
                if (episode.IsDbFilled)
                    episode.DbUpdate();
                else
                    episode.DbInsert();
            }

            // Check to see if the number of episode on Tvmaze equal the number in Tvmaze Local
            _epsByShowInDb = new List<int>();
            using (MariaDb mDb = new(_appInfo))
            {
                var rdr = mDb.ExecQuery($"select TvmEpisodeId from Episodes where TvmShowId = {showId}");
                while (rdr.Read())
                {
                    _epsByShowInDb.Add(int.Parse(rdr[0].ToString()!));
                }
            }
            if (_epsByShowInDb.Count > _epsByShowOnTvmaze.Count)
            {
                _log.Write($"More Episodes in DB {_epsByShowInDb.Count} than on TVMaze  {_epsByShowOnTvmaze.Count}", "", 0);
                _epsByShowInDb.Sort();
                _epsByShowOnTvmaze.Sort();
                var toDeleteEpisodes = _epsByShowInDb.Except(_epsByShowOnTvmaze);
                using Episode episodeToDelete = new(_appInfo);
                foreach (var episode in toDeleteEpisodes)
                {
                    episodeToDelete.FillViaDb(episode);
                    episodeToDelete.TvmEpisodeId = episode;
                    if (episodeToDelete.IsDbFilled) episodeToDelete.DbDelete();
                }
            }
            else if (_epsByShowInDb.Count < _epsByShowOnTvmaze.Count)
            {
                _log.Write($"Less Episodes in DB {_epsByShowInDb.Count} than on TVMaze  {_epsByShowOnTvmaze.Count} ############### Should not happen", "", 0);
            }
        }
    }
}