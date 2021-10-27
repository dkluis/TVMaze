using System;
using System.Collections.Generic;
using Common_Lib;

namespace Entities_Lib
{
    public class ShowAndEpisodes : IDisposable
    {
        private readonly AppInfo _appinfo;
        private List<Episode> _epsByShow;
        private TextFileHandler _log;
        private readonly Show _show;


        public ShowAndEpisodes(AppInfo appinfo)
        {
            _log = appinfo.TxtFile;
            _appinfo = appinfo;
            _show = new Show(_appinfo);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public void Refresh(int showid, bool ignore = false)
        {
            _show.FillViaTvmaze(showid);
            if (_show.IsDbFilled && _show.IsFollowed) _show.TvmStatus = "Following";
            _show.DbUpdate();
            _show.Reset();

            using (EpisodesByShow epsbyshow = new())
            {
                _epsByShow = epsbyshow.Find(_appinfo, showid);
            }

            foreach (var episode in _epsByShow)
                if (episode.IsDbFilled)
                    episode.DbUpdate();
                else
                    episode.DbInsert();
        }
    }
}