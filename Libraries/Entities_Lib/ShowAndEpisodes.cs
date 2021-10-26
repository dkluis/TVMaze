using System;
using System.Collections.Generic;
using Common_Lib;

namespace Entities_Lib
{
    public class ShowAndEpisodes : IDisposable
    {
        private readonly AppInfo appinfo;
        private List<Episode> EpsByShow;
        private TextFileHandler log;
        private readonly Show Show;


        public ShowAndEpisodes(AppInfo Appinfo)
        {
            log = Appinfo.TxtFile;
            appinfo = Appinfo;
            Show = new Show(appinfo);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public void Refresh(int showid, bool ignore = false)
        {
            Show.FillViaTvmaze(showid);
            if (Show.IsDbFilled && Show.IsFollowed) Show.TvmStatus = "Following";
            Show.DbUpdate();
            Show.Reset();

            using (EpisodesByShow epsbyshow = new())
            {
                EpsByShow = epsbyshow.Find(appinfo, showid);
            }

            foreach (var episode in EpsByShow)
                if (episode.isDBFilled)
                    episode.DbUpdate();
                else
                    episode.DbInsert();
        }
    }
}