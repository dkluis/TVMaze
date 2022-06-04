using System.Threading;

namespace TVMazeWeb.Data
{
    public class DataExchange
    {
        public string FunctionRequested;
        public string IntendedPage;
        public string SendingPage;
        public string LastPage;
        
        public string ShowName;

        public string ShowStateFunction;
        public string ShowStateShowName;
        public bool ShowStateIsActive;

        public string EpisodeStateFunction;
        public string EpisodeStateShowName;
        public string EpisodeStateSeason;
        public string EpisodeStateEpisode;
        public bool EpisodeStateIncludeShowRss;
        public bool EpisodeStateIsActive;

        public void Reset()
        {
            SendingPage = "";
            IntendedPage = "";
            ShowName = "";
        }

        public void ResetShowState()
        {
            ShowStateFunction = "";
            ShowStateShowName = "";
            ShowStateIsActive = false;
        }

        public void ResetEpisodeState()
        {
            EpisodeStateFunction = "";
            EpisodeStateSeason = "";
            EpisodeStateEpisode = "";
            EpisodeStateShowName = "";
            EpisodeStateIncludeShowRss = false;
            EpisodeStateIsActive = false;
        }
    }
}