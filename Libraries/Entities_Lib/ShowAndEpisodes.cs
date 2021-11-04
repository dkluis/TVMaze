using System;
using System.Collections.Generic;
using Common_Lib;

namespace Entities_Lib
{
    public class ShowAndEpisodes : IDisposable
    {
        private readonly AppInfo _appInfo;
        private readonly Show _show;
        private List<Episode> _epsByShow;

        public ShowAndEpisodes(AppInfo appInfo)
        {
            _appInfo = appInfo;
            _show = new Show(_appInfo);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public void Refresh(int showId, bool ignore = false)
        {
            _show.FillViaTvmaze(showId);
            if (_show.IsDbFilled && _show.IsFollowed) _show.TvmStatus = "Following";
            _show.DbUpdate();
            _show.Reset();
            using EpisodesByShow epsByShow = new();
            _epsByShow = epsByShow.Find(_appInfo, showId);
            foreach (var episode in _epsByShow)
                if (episode.IsDbFilled)
                    episode.DbUpdate();
                else
                    episode.DbInsert();
        }
    }
}