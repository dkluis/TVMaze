using Common_Lib;
using DB_Lib;
using Web_Lib;

using MySqlConnector;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;

namespace TvmEntities
{
    public class Episode : IDisposable
    {
        #region DB Record Definition

        public Int32  Id = 0;
        public Int32  TvmShowId = 0;
        public Int32  TvmEpisodeId = 0;
        public string TvmUrl = "";
        public string ShowName = "";
        public string SeasonEpisode = "";
        public int    SeasonNum = 0;
        public int    EpisodeNum = 0;
        public string BroadcastDate;
        public string PlexStatus = " ";
        public string PlexDate;

        #endregion

        #region Tvm Record Definiton (without what is in DB record)

        public string TvmType;
        public int    TvmRunTime;
        public string TvmImage;
        public string TvmSummary;

        #endregion

        public bool isFilled;
        public bool isDBFilled;
        public bool isJsonFilled;
        public bool isOnTvmaze;

        private readonly MariaDB Mdb;
        private readonly TextFileHandler log;
        private readonly AppInfo Appinfo;

        public Episode(AppInfo appinfo)
        {
            Appinfo = appinfo;
            Mdb = new(appinfo);
            log = appinfo.TxtFile;
        }

        public void Reset()
        {
            Id = 0;
            TvmShowId = 0;
            TvmUrl = "";
            ShowName = "";
            SeasonEpisode = "";
            SeasonNum = 0;
            EpisodeNum = 0;
            BroadcastDate = null;
            PlexStatus = " ";
            PlexDate = null;

            TvmType = "";
            TvmSummary = "";
            TvmImage = "";
            TvmRunTime = 0;

            isFilled = false;
            isJsonFilled = false;
            isDBFilled = false;
            isOnTvmaze = false;
        }

        public void FillViaTvmaze(int episodeid)
        {
            using (WebAPI je = new(Appinfo))
            {
                FillViaJson(je.ConvertHttpToJObject(je.GetEpisode(episodeid)));
                // FillViaDb(showid);  // Might not be necessary unless some show info needs to be added to the episode table
            }   
        }

        private void FillViaJson(JObject episode)
        {
            Id = int.Parse(episode["id"].ToString());
            BroadcastDate = episode["airdate"].ToString();

            //TODO also search for the watched, downloaded, etc statuses
            PlexStatus = " ";
            PlexDate = null;

            TvmShowId = int.Parse(episode["_embedded"]["show"]["id"].ToString());
            ShowName = episode["_embedded"]["show"]["name"].ToString();

            TvmUrl = episode["url"].ToString();

            SeasonNum = int.Parse(episode["season"].ToString());
            EpisodeNum = int.Parse(episode["number"].ToString());      
            SeasonEpisode = Common.BuildSeasonEpisodeString(SeasonNum, EpisodeNum);
        

            TvmType = episode["type"].ToString();
            TvmSummary = episode["summary"].ToString();
            if (episode["image"] is not null) { TvmImage = episode["image"]["medium"].ToString(); }
            TvmRunTime = int.Parse(episode["runtime"].ToString());
        }    

        private void FillViaDb(int showid)
        {
            TvmShowId = showid;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

    }
}
