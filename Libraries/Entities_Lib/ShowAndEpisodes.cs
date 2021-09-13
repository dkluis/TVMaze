using System;
using System.Collections.Generic;
using Common_Lib;

namespace Entities_Lib
{
    public class ShowAndEpisodes : IDisposable
    {
        private Show Show;
        private TextFileHandler log;
        private List<Episode> EpsByShow;
        private AppInfo appinfo;


        public ShowAndEpisodes(AppInfo Appinfo)
        {
            log = Appinfo.TxtFile;
            appinfo = Appinfo;
            Show = new(appinfo);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public void Refresh(int showid, bool ignore = false)
        {
            Show.FillViaTvmaze(showid);
            if (Show.isDBFilled && Show.isFollowed) { Show.TvmStatus = "Following"; }
            Show.DbUpdate();
            Show.Reset();

            using (EpisodesByShow epsbyshow = new()) { EpsByShow = epsbyshow.Find(appinfo, showid); }
            foreach (Episode episode in EpsByShow )
            {
                if (episode.isDBFilled) { episode.DbUpdate(); } else { episode.DbInsert();  }
            }

        }
    }
}
